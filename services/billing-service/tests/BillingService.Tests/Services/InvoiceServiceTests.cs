using BillingService.Api.Clients;
using BillingService.Api.Data;
using BillingService.Api.DTOs;
using BillingService.Api.Exceptions;
using BillingService.Api.Models;
using BillingService.Api.Services;
using BillingService.Api.Validators;
using Korp.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BillingService.Tests.Services;

public class InvoiceServiceTests
{
    [Fact]
    public async Task CreateAsync_RejectsRequestWithoutItems()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new FakeInventoryClient());

        var exception = await Assert.ThrowsAsync<InvoiceHasNoItemsException>(() =>
            service.CreateAsync(new CreateInvoiceRequest()));

        Assert.Equal(400, exception.StatusCode);
        Assert.Empty(await context.Invoices.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsTooManyItemsBeforeCallingInventory()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new FakeInventoryClient());
        var request = new CreateInvoiceRequest
        {
            Items = Enumerable.Range(0, CreateInvoiceRequestValidator.MaximumItems + 1)
                .Select(_ => new CreateInvoiceItemRequest
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1m,
                    UnitPrice = 1m
                })
                .ToList()
        };

        var exception = await Assert.ThrowsAsync<TooManyInvoiceItemsException>(() =>
            service.CreateAsync(request));

        Assert.Equal(400, exception.StatusCode);
        Assert.Empty(await context.Invoices.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsNullItemWithoutThrowingNullReference()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new FakeInventoryClient());
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest> { null! }
        };

        var exception = await Assert.ThrowsAsync<InvalidInvoiceItemException>(() =>
            service.CreateAsync(request));

        Assert.Equal(400, exception.StatusCode);
        Assert.Empty(await context.Invoices.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnrepresentableTotalBeforePersistence()
    {
        await using var context = CreateContext();
        var productId = Guid.NewGuid();
        var service = CreateService(context, InventoryContaining(productId));
        var request = RequestWithItem(
            productId,
            quantity: InvoiceAmount.MaxValue,
            unitPrice: 2m);

        var exception = await Assert.ThrowsAsync<InvoiceAmountOutOfRangeException>(() =>
            service.CreateAsync(request));

        Assert.Equal(400, exception.StatusCode);
        Assert.Empty(await context.Invoices.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenProductDoesNotExist_RejectsAndDoesNotPersistInvoice()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new FakeInventoryClient());
        var missingProductId = Guid.NewGuid();
        var request = RequestWithItem(missingProductId, quantity: 1m, unitPrice: 5m);

        var exception = await Assert.ThrowsAsync<InventoryProductNotFoundException>(() =>
            service.CreateAsync(request));

        Assert.Equal(404, exception.StatusCode);
        Assert.Contains(missingProductId.ToString(), exception.Message);
        Assert.Empty(await context.Invoices.ToListAsync());
        Assert.Empty(await context.InvoiceItems.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_CreatesOpenInvoiceAndCopiesProductSnapshot()
    {
        await using var context = CreateContext();
        var productId = Guid.NewGuid();
        var inventory = new FakeInventoryClient();
        inventory.Products[productId] = new InventoryProductDto
        {
            Id = productId,
            Code = "PRD-10",
            Description = "Produto do estoque",
            StockBalance = 100m,
            Unit = "UN"
        };
        var service = CreateService(context, inventory);
        var request = RequestWithItem(productId, quantity: 2.5m, unitPrice: 12.40m) with
        {
            CustomerName = "  Cliente Teste  ",
            Notes = "  Observacao da nota  "
        };

        var response = await service.CreateAsync(request);

        Assert.Equal(1, response.InvoiceNumber);
        Assert.Equal("Open", response.Status);
        Assert.Equal("Cliente Teste", response.CustomerName);
        Assert.Equal("Observacao da nota", response.Notes);
        Assert.Equal(31.000m, response.TotalAmount);
        var responseItem = Assert.Single(response.Items);
        Assert.Equal(productId, responseItem.ProductId);
        Assert.Equal("PRD-10", responseItem.ProductCode);
        Assert.Equal("Produto do estoque", responseItem.ProductDescription);
        Assert.Equal(31.000m, responseItem.TotalPrice);

        context.ChangeTracker.Clear();
        var stored = await context.Invoices.Include(invoice => invoice.Items).SingleAsync();
        Assert.Equal(InvoiceStatus.Open, stored.Status);
        Assert.Equal(31.000m, stored.TotalAmount);
        Assert.Equal("PRD-10", Assert.Single(stored.Items).ProductCode);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleItems_SumsAllLineTotals()
    {
        await using var context = CreateContext();
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var inventory = InventoryContaining(firstProductId);
        inventory.Products[secondProductId] = new InventoryProductDto
        {
            Id = secondProductId,
            Code = "PRD-02",
            Description = "Segundo produto",
            StockBalance = 50m,
            Unit = "KG"
        };
        var service = CreateService(context, inventory);
        var request = new CreateInvoiceRequest
        {
            Items = new List<CreateInvoiceItemRequest>
            {
                new() { ProductId = firstProductId, Quantity = 2m, UnitPrice = 10m },
                new() { ProductId = secondProductId, Quantity = 1.5m, UnitPrice = 4m }
            }
        };

        var response = await service.CreateAsync(request);

        Assert.Equal(26m, response.TotalAmount);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(new[] { 20m, 6m }, response.Items.Select(item => item.TotalPrice));
        Assert.Equal(new[] { "PRD-01", "PRD-02" }, response.Items.Select(item => item.ProductCode));

        context.ChangeTracker.Clear();
        var stored = await context.Invoices.Include(invoice => invoice.Items).SingleAsync();
        Assert.Equal(2, stored.Items.Count);
        Assert.Equal(26m, stored.Items.Sum(item => item.TotalPrice));
    }

    [Fact]
    public async Task CreateAsync_AssignsSequentialNumbersAcrossSerialCreates()
    {
        await using var context = CreateContext();
        var productId = Guid.NewGuid();
        var inventory = InventoryContaining(productId);
        var service = CreateService(context, inventory);

        var first = await service.CreateAsync(RequestWithItem(productId));
        var second = await service.CreateAsync(RequestWithItem(productId));

        Assert.Equal(1, first.InvoiceNumber);
        Assert.Equal(2, second.InvoiceNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WhenInvoiceDoesNotExist_ThrowsNotFound()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new FakeInventoryClient());
        var id = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvoiceNotFoundException>(() => service.GetByIdAsync(id));

        Assert.Equal(404, exception.StatusCode);
        Assert.Contains(id.ToString(), exception.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsNewestInvoiceFirstWithSummaryData()
    {
        await using var context = CreateContext();
        var older = InvoiceWithItem(invoiceNumber: 1, createdAt: DateTime.UtcNow.AddHours(-1));
        var newer = InvoiceWithItem(invoiceNumber: 2, createdAt: DateTime.UtcNow);
        context.Invoices.AddRange(older, newer);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeInventoryClient());

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(new[] { 2, 1 }, result.Select(invoice => invoice.InvoiceNumber));
        Assert.All(result, invoice => Assert.Equal(1, invoice.ItemCount));
    }

    [Fact]
    public async Task PrintAsync_WhenInvoiceDoesNotExist_ThrowsNotFound()
    {
        await using var context = CreateContext();
        var service = CreateService(context, new FakeInventoryClient());

        var exception = await Assert.ThrowsAsync<InvoiceNotFoundException>(() =>
            service.PrintAsync(Guid.NewGuid(), "print-key"));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task PrintAsync_WhenKeyBelongsToAnotherInvoice_ThrowsConflict()
    {
        await using var context = CreateContext();
        var keyedInvoice = InvoiceWithItem(invoiceNumber: 1, status: InvoiceStatus.Closed, idempotencyKey: "used-key");
        var targetInvoice = InvoiceWithItem(invoiceNumber: 2);
        context.Invoices.AddRange(keyedInvoice, targetInvoice);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeInventoryClient());

        var exception = await Assert.ThrowsAsync<DuplicateIdempotencyKeyException>(() =>
            service.PrintAsync(targetInvoice.Id, "used-key"));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(InvoiceStatus.Open, targetInvoice.Status);
    }

    [Fact]
    public async Task PrintAsync_WhenInvoiceIsClosedWithDifferentKey_RejectsStatus()
    {
        await using var context = CreateContext();
        var invoice = InvoiceWithItem(
            invoiceNumber: 12,
            status: InvoiceStatus.Closed,
            idempotencyKey: "original-key");
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeInventoryClient());

        var exception = await Assert.ThrowsAsync<InvalidInvoiceStatusException>(() =>
            service.PrintAsync(invoice.Id, "another-key"));

        Assert.Equal(409, exception.StatusCode);
        Assert.Contains("Closed", exception.Message);
    }

    [Fact]
    public async Task PrintAsync_WhenSameKeyAlreadyClosed_ReturnsExistingInvoiceWithoutPublishingAgain()
    {
        await using var context = CreateContext();
        var invoice = InvoiceWithItem(
            invoiceNumber: 14,
            status: InvoiceStatus.Closed,
            idempotencyKey: "same-key");
        invoice.PrintedAt = DateTime.UtcNow.AddMinutes(-1);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        var publisher = PublisherMock();
        var service = CreateService(context, new FakeInventoryClient(), publisher);

        var response = await service.PrintAsync(invoice.Id, "same-key");

        Assert.Equal("Closed", response.Status);
        Assert.Equal(invoice.PrintedAt, response.PrintedAt);
        publisher.Verify(
            endpoint => endpoint.Publish(It.IsAny<InvoicePrintedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PrintAsync_ClosesInvoicePersistsKeyAndPublishesInvoiceItems()
    {
        await using var context = CreateContext();
        var invoice = InvoiceWithItem(invoiceNumber: 21);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        InvoicePrintedEvent? publishedEvent = null;
        var publisher = PublisherMock();
        publisher
            .Setup(endpoint => endpoint.Publish(It.IsAny<InvoicePrintedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<InvoicePrintedEvent, CancellationToken>((message, _) => publishedEvent = message)
            .Returns(Task.CompletedTask);
        var service = CreateService(context, new FakeInventoryClient(), publisher);
        var beforePrint = DateTime.UtcNow;

        var response = await service.PrintAsync(invoice.Id, "print-21");

        Assert.Equal("Closed", response.Status);
        Assert.NotNull(response.PrintedAt);
        Assert.True(response.PrintedAt >= beforePrint);
        Assert.NotNull(publishedEvent);
        Assert.Equal(invoice.Id, publishedEvent.InvoiceId);
        Assert.Equal("21", publishedEvent.InvoiceNumber);
        var eventItem = Assert.Single(publishedEvent.Items);
        var invoiceItem = Assert.Single(invoice.Items);
        Assert.Equal(invoiceItem.ProductId, eventItem.ProductId);
        Assert.Equal(invoiceItem.Quantity, eventItem.Quantity);
        publisher.Verify(
            endpoint => endpoint.Publish(It.IsAny<InvoicePrintedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        context.ChangeTracker.Clear();
        var stored = await context.Invoices.SingleAsync(storedInvoice => storedInvoice.Id == invoice.Id);
        Assert.Equal(InvoiceStatus.Closed, stored.Status);
        Assert.Equal("print-21", stored.IdempotencyKey);
        Assert.NotNull(stored.PrintedAt);
    }

    private static BillingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase($"billing-tests-{Guid.NewGuid()}")
            .Options;

        return new BillingDbContext(options);
    }

    private static InvoiceService CreateService(
        BillingDbContext context,
        IInventoryClient inventoryClient,
        Mock<IPublishEndpoint>? publisher = null)
    {
        publisher ??= PublisherMock();

        return new InvoiceService(
            context,
            inventoryClient,
            publisher.Object,
            NullLogger<InvoiceService>.Instance);
    }

    private static Mock<IPublishEndpoint> PublisherMock()
    {
        var publisher = new Mock<IPublishEndpoint>();
        publisher
            .Setup(endpoint => endpoint.Publish(It.IsAny<InvoicePrintedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return publisher;
    }

    private static FakeInventoryClient InventoryContaining(Guid productId)
    {
        var inventory = new FakeInventoryClient();
        inventory.Products[productId] = new InventoryProductDto
        {
            Id = productId,
            Code = "PRD-01",
            Description = "Produto",
            StockBalance = 100m,
            Unit = "UN"
        };
        return inventory;
    }

    private static CreateInvoiceRequest RequestWithItem(
        Guid productId,
        decimal quantity = 1m,
        decimal unitPrice = 10m) => new()
        {
            Items = new List<CreateInvoiceItemRequest>
        {
            new()
            {
                ProductId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice
            }
        }
        };

    private static Invoice InvoiceWithItem(
        int invoiceNumber,
        DateTime? createdAt = null,
        InvoiceStatus status = InvoiceStatus.Open,
        string? idempotencyKey = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            Status = status,
            IdempotencyKey = idempotencyKey,
            TotalAmount = 20m,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = createdAt ?? DateTime.UtcNow
        };
        invoice.Items.Add(new InvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Invoice = invoice,
            ProductId = Guid.NewGuid(),
            ProductCode = "PRD-01",
            ProductDescription = "Produto",
            Quantity = 2m,
            UnitPrice = 10m
        });
        return invoice;
    }

    private sealed class FakeInventoryClient : IInventoryClient
    {
        public Dictionary<Guid, InventoryProductDto> Products { get; } = new();

        public Task<InventoryProductDto?> GetProductAsync(Guid productId, CancellationToken ct = default)
        {
            Products.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }

        public Task<InventoryStockBalanceDto?> GetStockBalanceAsync(
            Guid productId,
            CancellationToken ct = default) => Task.FromResult<InventoryStockBalanceDto?>(null);

        public Task<InventoryStockBalanceDto?> DeductStockAsync(
            Guid productId,
            decimal quantity,
            string invoiceReference,
            CancellationToken ct = default) => Task.FromResult<InventoryStockBalanceDto?>(null);
    }
}
