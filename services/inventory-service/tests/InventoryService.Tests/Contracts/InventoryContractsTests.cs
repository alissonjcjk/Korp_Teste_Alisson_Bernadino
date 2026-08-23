using InventoryService.Api.Data;
using InventoryService.Api.DTOs;
using InventoryService.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace InventoryService.Tests.Contracts;

public class InventoryContractsTests
{
    [Fact]
    public void ProductModel_ConfiguresUniqueCodeConcurrencyTokenAndStockPrecision()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase("inventory-model-contract")
            .Options;
        using var context = new InventoryDbContext(options);

        var product = context.Model.FindEntityType(typeof(Product));

        Assert.NotNull(product);
        Assert.Contains(product.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Product.Code)]));

        var rowVersion = product.FindProperty(nameof(Product.RowVersion));
        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);

        var stockBalance = product.FindProperty(nameof(Product.StockBalance));
        Assert.NotNull(stockBalance);
        Assert.Equal(18, stockBalance.GetPrecision());
        Assert.Equal(4, stockBalance.GetScale());
    }

    [Fact]
    public void ApiResponseOk_PreservesDataAndSuccessMessage()
    {
        var data = new ProductResponse
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Code = "PROD-001",
            Description = "Produto",
            StockBalance = 10,
            Unit = "UN"
        };

        var response = ApiResponse<ProductResponse>.Ok(data, "Criado");

        Assert.True(response.Success);
        Assert.Same(data, response.Data);
        Assert.Equal("Criado", response.Message);
    }

}
