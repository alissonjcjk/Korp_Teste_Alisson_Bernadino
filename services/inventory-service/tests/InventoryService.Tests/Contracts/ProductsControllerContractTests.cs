using System.Reflection;
using InventoryService.Api.Controllers;
using InventoryService.Api.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Tests.Contracts;

public class ProductsControllerContractTests
{
    [Fact]
    public void Actions_DescribeEveryErrorResponseWithTheStandardEnvelope()
    {
        var errorResponses = typeof(ProductsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>())
            .Where(attribute => attribute.StatusCode >= StatusCodes.Status400BadRequest)
            .ToArray();

        Assert.NotEmpty(errorResponses);
        Assert.All(errorResponses, response =>
            Assert.Equal(typeof(ApiErrorResponse), response.Type));
    }

    [Theory]
    [InlineData(nameof(ProductsController.Create))]
    [InlineData(nameof(ProductsController.Update))]
    [InlineData(nameof(ProductsController.DeductStock))]
    public void ActionsWithBody_ConsumeJsonAndDescribeUnsupportedMediaType(string actionName)
    {
        var action = typeof(ProductsController).GetMethod(actionName)!;
        var consumes = action.GetCustomAttribute<ConsumesAttribute>();
        var unsupportedMediaType = action
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .SingleOrDefault(attribute =>
                attribute.StatusCode == StatusCodes.Status415UnsupportedMediaType);

        Assert.NotNull(consumes);
        Assert.Contains("application/json", consumes.ContentTypes);
        Assert.NotNull(unsupportedMediaType);
        Assert.Equal(typeof(ApiErrorResponse), unsupportedMediaType.Type);
    }
}
