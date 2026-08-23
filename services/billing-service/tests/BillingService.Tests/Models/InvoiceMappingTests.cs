using BillingService.Api.DTOs;
using BillingService.Api.Models;

namespace BillingService.Tests.Models;

public class InvoiceMappingTests
{
    [Fact]
    public void InvoiceItem_TotalPrice_MultipliesQuantityByUnitPrice()
    {
        var item = new InvoiceItem
        {
            Quantity = 2.5m,
            UnitPrice = 4.20m
        };

        Assert.Equal(10.500m, item.TotalPrice);
    }

    [Fact]
    public void InvoiceItem_TotalPrice_RoundsToStoredScaleAwayFromZero()
    {
        var item = new InvoiceItem
        {
            Quantity = 1.0001m,
            UnitPrice = 1.0001m
        };

        Assert.Equal(1.0002m, item.TotalPrice);
    }

    [Fact]
    public void ToResponse_MapsInvoiceAndCalculatesEachItemTotal()
    {
        var createdAt = new DateTime(2026, 8, 20, 10, 30, 0, DateTimeKind.Utc);
        var updatedAt = createdAt.AddMinutes(5);
        var printedAt = createdAt.AddMinutes(4);
        var productId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 27,
            Status = InvoiceStatus.Closed,
            CustomerName = "Cliente",
            Notes = "Observacao",
            TotalAmount = 19.98m,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            PrintedAt = printedAt,
            Items = new List<InvoiceItem>
            {
                new()
                {
                    Id = itemId,
                    ProductId = productId,
                    ProductCode = "PRD-01",
                    ProductDescription = "Produto de teste",
                    Quantity = 2m,
                    UnitPrice = 9.99m
                }
            }
        };

        var response = invoice.ToResponse();

        Assert.Equal(invoice.Id, response.Id);
        Assert.Equal(27, response.InvoiceNumber);
        Assert.Equal("Closed", response.Status);
        Assert.Equal("Cliente", response.CustomerName);
        Assert.Equal("Observacao", response.Notes);
        Assert.Equal(19.98m, response.TotalAmount);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);
        Assert.Equal(printedAt, response.PrintedAt);
        var item = Assert.Single(response.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("PRD-01", item.ProductCode);
        Assert.Equal("Produto de teste", item.ProductDescription);
        Assert.Equal(2m, item.Quantity);
        Assert.Equal(9.99m, item.UnitPrice);
        Assert.Equal(19.98m, item.TotalPrice);
    }

    [Fact]
    public void ToSummary_MapsStatusAndCountsItems()
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = 8,
            Status = InvoiceStatus.Open,
            CustomerName = "Cliente",
            TotalAmount = 30m,
            CreatedAt = DateTime.UtcNow,
            Items = new List<InvoiceItem>
            {
                new(),
                new(),
                new()
            }
        };

        var summary = invoice.ToSummary();

        Assert.Equal(invoice.Id, summary.Id);
        Assert.Equal(8, summary.InvoiceNumber);
        Assert.Equal("Open", summary.Status);
        Assert.Equal("Cliente", summary.CustomerName);
        Assert.Equal(30m, summary.TotalAmount);
        Assert.Equal(3, summary.ItemCount);
        Assert.Equal(invoice.CreatedAt, summary.CreatedAt);
    }
}
