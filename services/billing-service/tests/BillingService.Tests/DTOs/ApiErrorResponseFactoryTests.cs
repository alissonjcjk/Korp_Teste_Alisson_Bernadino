using System.Text;
using System.Text.Json;
using BillingService.Api.Configuration;
using BillingService.Api.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BillingService.Tests.DTOs;

public class ApiErrorResponseFactoryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Create_UsesExactEnvelopeAndOmitsErrorsWhenAbsent()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-factory" };

        var response = ApiErrorResponseFactory.Create(context, 500, "Erro genérico.");
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonOptions));
        var root = document.RootElement;

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(500, root.GetProperty("statusCode").GetInt32());
        Assert.Equal("Erro genérico.", root.GetProperty("message").GetString());
        Assert.Equal("trace-factory", root.GetProperty("traceId").GetString());
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.False(root.TryGetProperty("errors", out _));
        Assert.Equal(5, root.EnumerateObject().Count());
        Assert.Equal(TimeSpan.Zero, response.Timestamp.Offset);
    }

    [Fact]
    public void FromModelState_GroupsValidationErrorsWithoutExceptionDetails()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-validation" };
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Items[0].Quantity", "A quantidade é obrigatória.");
        modelState.AddModelError("Items[0].Quantity", "A quantidade deve ser maior que zero.");
        modelState.AddModelError("Items[0].Quantity", "A quantidade é obrigatória.");
        modelState.SetModelValue("request", new ValueProviderResult("invalid"));
        modelState["request"]!.Errors.Add(new ModelError(new FormatException("conversion-secret")));

        var response = ApiErrorResponseFactory.FromModelState(context, modelState);
        var json = JsonSerializer.Serialize(response, JsonOptions);

        Assert.Equal(400, response.StatusCode);
        Assert.Equal(ApiErrorResponseFactory.ValidationMessage, response.Message);
        Assert.Equal("trace-validation", response.TraceId);
        Assert.NotNull(response.Errors);
        Assert.Equal(
            new[] { "A quantidade é obrigatória.", "A quantidade deve ser maior que zero." },
            response.Errors["Items[0].Quantity"]);
        Assert.Equal(new[] { "O valor informado é inválido." }, response.Errors["request"]);
        Assert.DoesNotContain("conversion-secret", json);
        Assert.DoesNotContain("exception", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfiguredJsonFormatter_DoesNotExposeDeserializationDetails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredApiControllers();
        await using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<JsonOptions>>().Value;
        Assert.False(options.AllowInputFormatterExceptionMessages);

        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "billing-json-trace"
        };
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            """
            {
              "items": [{
                "productId": "11111111-1111-1111-1111-111111111111",
                "quantity": "deserializer-secret",
                "unitPrice": 1
              }]
            }
            """));

        var modelState = new ModelStateDictionary();
        var metadata = new EmptyModelMetadataProvider()
            .GetMetadataForType(typeof(CreateInvoiceRequest));
        var formatter = new SystemTextJsonInputFormatter(
            options,
            NullLogger<SystemTextJsonInputFormatter>.Instance);
        var formatterContext = new InputFormatterContext(
            httpContext,
            string.Empty,
            modelState,
            metadata,
            (stream, encoding) => new StreamReader(stream, encoding));

        var formatterResult = await formatter.ReadAsync(formatterContext);
        var response = ApiErrorResponseFactory.FromModelState(httpContext, modelState);
        var json = JsonSerializer.Serialize(response, JsonOptions);

        Assert.True(formatterResult.HasError);
        Assert.Contains("O valor informado é inválido.", response.Errors!.SelectMany(entry => entry.Value));
        Assert.DoesNotContain("deserializer-secret", json);
        Assert.DoesNotContain("System.Nullable", json);
        Assert.DoesNotContain("LineNumber", json);
        Assert.DoesNotContain("BytePositionInLine", json);
    }
}
