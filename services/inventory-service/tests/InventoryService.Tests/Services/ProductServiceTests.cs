using InventoryService.Api.Data;
using InventoryService.Api.DTOs;
using InventoryService.Api.Exceptions;
using InventoryService.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryService.Tests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesAndPersistsProduct()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var response = await service.CreateAsync(
            new CreateProductRequest
            {
                Code = "  prod-001  ",
                Description = "  Produto de teste  ",
                StockBalance = 12.5m,
                Unit = "  un  "
            },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("PROD-001", response.Code);
        Assert.Equal("Produto de teste", response.Description);
        Assert.Equal(12.5m, response.StockBalance);
        Assert.Equal("UN", response.Unit);

        var persisted = await context.Products.SingleAsync(CancellationToken.None);
        Assert.Equal(response.Id, persisted.Id);
        Assert.Equal(response.Code, persisted.Code);
        Assert.Equal(response.Description, persisted.Description);
        Assert.Equal(response.StockBalance, persisted.StockBalance);
        Assert.Equal(response.Unit, persisted.Unit);
    }

    [Fact]
    public async Task CreateAsync_WithExistingCodeIgnoringCase_ThrowsAndDoesNotInsertDuplicate()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.CreateAsync(
            CreateRequest("prod-001", "Primeiro produto", 5),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DuplicateProductCodeException>(() =>
            service.CreateAsync(
                CreateRequest("PROD-001", "Produto duplicado", 7),
                CancellationToken.None));

        Assert.Contains("PROD-001", exception.Message);
        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(1, await context.Products.CountAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCodeAndProjectsResponses()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.CreateAsync(
            CreateRequest("PROD-002", "Segundo", 2),
            CancellationToken.None);
        await service.CreateAsync(
            CreateRequest("PROD-001", "Primeiro", 1),
            CancellationToken.None);

        var products = (await service.GetAllAsync(
            searchTerm: null,
            CancellationToken.None)).ToArray();

        Assert.Equal(2, products.Length);
        Assert.Equal("PROD-001", products[0].Code);
        Assert.Equal("PROD-002", products[1].Code);
        Assert.Equal([1m, 2m], products.Select(product => product.StockBalance));
    }

    [Theory]
    [InlineData("prod-002")]
    [InlineData("segUNDO")]
    public async Task GetAllAsync_FiltersCodeOrDescriptionIgnoringCase(string searchTerm)
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        await service.CreateAsync(
            CreateRequest("PROD-001", "Primeiro item", 1),
            CancellationToken.None);
        await service.CreateAsync(
            CreateRequest("PROD-002", "Segundo item", 2),
            CancellationToken.None);

        var products = (await service.GetAllAsync(
            searchTerm,
            CancellationToken.None)).ToArray();

        var product = Assert.Single(products);
        Assert.Equal("PROD-002", product.Code);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ThrowsProductNotFound()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var missingId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var exception = await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            service.GetByIdAsync(missingId, CancellationToken.None));

        Assert.Equal(404, exception.StatusCode);
        Assert.Contains(missingId.ToString(), exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ChangesDescriptionAndUnitButPreservesCodeAndBalance()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("PROD-001", "Original", 8),
            CancellationToken.None);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateProductRequest
            {
                Description = "  Atualizado  ",
                Unit = "  cx  "
            },
            CancellationToken.None);

        Assert.Equal("PROD-001", updated.Code);
        Assert.Equal("Atualizado", updated.Description);
        Assert.Equal("CX", updated.Unit);
        Assert.Equal(8m, updated.StockBalance);
    }

    [Fact]
    public async Task DeductStockAsync_WithAvailableStock_UpdatesBalance()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("PROD-001", "Produto", 10),
            CancellationToken.None);

        var balance = await service.DeductStockAsync(
            created.Id,
            new DeductStockRequest
            {
                Quantity = 3.5m,
                InvoiceReference = "NF-100"
            },
            CancellationToken.None);

        Assert.Equal(6.5m, balance.StockBalance);
        Assert.Equal(6.5m, (await context.Products.SingleAsync(
            CancellationToken.None)).StockBalance);
    }

    [Fact]
    public async Task DeductStockAsync_WithInsufficientStock_ThrowsAndPreservesBalance()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("PROD-001", "Produto", 2),
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InsufficientStockException>(() =>
            service.DeductStockAsync(
                created.Id,
                new DeductStockRequest
                {
                    Quantity = 3,
                    InvoiceReference = "NF-100"
                },
                CancellationToken.None));

        Assert.Contains("Disponível: 2", exception.Message);
        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(2m, (await context.Products.SingleAsync(
            CancellationToken.None)).StockBalance);
    }

    [Fact]
    public async Task DeductStockAsync_WhenInvoiceReferenceIsRepeated_DeductsAgain_AsCurrentBehavior()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("PROD-001", "Produto", 10),
            CancellationToken.None);
        var request = new DeductStockRequest { Quantity = 2, InvoiceReference = "NF-100" };

        await service.DeductStockAsync(
            created.Id,
            request,
            CancellationToken.None);
        var secondResponse = await service.DeductStockAsync(
            created.Id,
            request,
            CancellationToken.None);

        Assert.Equal(6m, secondResponse.StockBalance);
        Assert.Equal(6m, (await context.Products.SingleAsync(
            CancellationToken.None)).StockBalance);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProduct_RemovesIt()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(
            CreateRequest("PROD-001", "Produto", 10),
            CancellationToken.None);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        Assert.Empty(await context.Products.ToListAsync(CancellationToken.None));
    }

    private static InventoryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase($"inventory-service-tests-{Guid.NewGuid():N}")
            .Options;

        return new InventoryDbContext(options);
    }

    private static ProductService CreateService(InventoryDbContext context) =>
        new(context, NullLogger<ProductService>.Instance);

    private static CreateProductRequest CreateRequest(
        string code,
        string description,
        decimal balance) => new()
        {
            Code = code,
            Description = description,
            StockBalance = balance,
            Unit = "UN"
        };
}
