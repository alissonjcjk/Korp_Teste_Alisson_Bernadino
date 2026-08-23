using System.Text;
using System.Text.Json;
using InventoryService.Api.Configuration;
using InventoryService.Api.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InventoryService.Tests.Contracts;

public class ApiErrorResponseFactoryTests
{
    [Fact]
    public void FromModelState_GroupsMessagesByField()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "inventory-validation-trace"
        };
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("StockBalance", "O saldo inicial é obrigatório.");
        modelState.AddModelError("StockBalance", "O saldo inicial é obrigatório.");
        modelState.AddModelError("StockBalance", "O saldo inicial é inválido.");
        modelState.AddModelError("Unit", "A unidade é obrigatória.");
        var response = ApiErrorResponseFactory.FromModelState(httpContext, modelState);

        Assert.False(response.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(ApiErrorResponseFactory.ValidationMessage, response.Message);
        Assert.Equal("inventory-validation-trace", response.TraceId);
        Assert.NotNull(response.Errors);
        Assert.Equal(
            ["O saldo inicial é obrigatório.", "O saldo inicial é inválido."],
            response.Errors["StockBalance"]);
        Assert.Equal(["A unidade é obrigatória."], response.Errors["Unit"]);
    }

    [Fact]
    public void FromModelState_DoesNotExposeBindingExceptionMessage()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "inventory-binding-trace"
        };
        var modelState = new ModelStateDictionary();
        modelState.SetModelValue("Quantity", new ValueProviderResult("inválido"));
        modelState["Quantity"]!.Errors.Add(new ModelError(
            new InvalidOperationException("segredo interno do model binder")));
        var response = ApiErrorResponseFactory.FromModelState(httpContext, modelState);

        Assert.NotNull(response.Errors);
        Assert.Equal(["O valor informado é inválido."], response.Errors["Quantity"]);
        Assert.DoesNotContain("segredo interno", response.Errors["Quantity"][0]);
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
            TraceIdentifier = "inventory-json-trace"
        };
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
            """
            { "stockBalance": "deserializer-secret" }
            """));

        var modelState = new ModelStateDictionary();
        var metadata = new EmptyModelMetadataProvider()
            .GetMetadataForType(typeof(CreateProductRequest));
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
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.True(formatterResult.HasError);
        Assert.Contains("O valor informado é inválido.", response.Errors!.SelectMany(entry => entry.Value));
        Assert.DoesNotContain("deserializer-secret", json);
        Assert.DoesNotContain("System.Nullable", json);
        Assert.DoesNotContain("LineNumber", json);
        Assert.DoesNotContain("BytePositionInLine", json);
    }

    [Theory]
    [InlineData(StatusCodes.Status404NotFound, "O recurso solicitado não foi encontrado.")]
    [InlineData(
        StatusCodes.Status405MethodNotAllowed,
        "O método HTTP informado não é permitido para este recurso.")]
    [InlineData(
        StatusCodes.Status415UnsupportedMediaType,
        "O formato do conteúdo enviado não é suportado.")]
    public void FromStatusCode_CreatesStandardErrorContract(int statusCode, string message)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "inventory-status-code-trace"
        };

        var response = ApiErrorResponseFactory.FromStatusCode(context, statusCode);

        Assert.False(response.Success);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Errors);
        Assert.Equal("inventory-status-code-trace", response.TraceId);
        Assert.Equal(TimeSpan.Zero, response.Timestamp.Offset);
    }
}
