using BillingService.Api.DTOs;
using BillingService.Api.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BillingService.Tests.DTOs;

public class OpenApiRequiredPropertiesTests
{
    [Fact]
    public void InputDtos_ExposeFluentValidationRequiredFieldsInOpenApi()
    {
        AssertRequired<CreateInvoiceItemRequest>("productId", "quantity", "unitPrice");
        AssertRequired<CreateInvoiceRequest>("items");
    }

    private static void AssertRequired<T>(params string[] propertyNames)
    {
        var schema = new OpenApiSchema
        {
            Properties = propertyNames.ToDictionary(
                propertyName => propertyName,
                _ => new OpenApiSchema { Nullable = true })
        };
        var context = new SchemaFilterContext(
            typeof(T),
            null!,
            new SchemaRepository(),
            null!,
            null!);

        new RequiredPropertiesSchemaFilter().Apply(schema, context);

        Assert.Equal(propertyNames.Order(), schema.Required.Order());
        Assert.All(propertyNames, propertyName =>
            Assert.False(schema.Properties[propertyName].Nullable));
    }
}
