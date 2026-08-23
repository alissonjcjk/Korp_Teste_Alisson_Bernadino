using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BillingService.Api.Configuration;
using BillingService.Api.DTOs;
using BillingService.Api.Middleware;
using BillingService.Api.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Tests.DTOs;

public class ApiPipelineContractTests
{
    [Fact]
    public async Task Post_WithNullItem_ReturnsValidationInsteadOfServerError()
    {
        var response = await ExecuteAsync(
            "POST",
            "/__contract/billing",
            """{ "items": [null] }""",
            "application/json");

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("O item da nota fiscal não pode ser nulo.", response.Body.GetRawText());
        Assert.True(response.Body.GetProperty("errors").TryGetProperty("Items[0]", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    public async Task Post_WithMissingOrNullBody_ReturnsPortugueseValidationEnvelope(string body)
    {
        var response = await ExecuteAsync(
            "POST",
            "/__contract/billing",
            body,
            "application/json");

        Assert.Equal(400, response.StatusCode);
        Assert.Equal("Um ou mais erros de validação ocorreram.",
            response.Body.GetProperty("message").GetString());
        Assert.Contains("O corpo da requisição é obrigatório.", response.Body.GetRawText());
    }

    [Fact]
    public async Task Post_WithInvalidDecimal_DoesNotExposeFormatterDetails()
    {
        const string secret = "deserializer-secret";
        var response = await ExecuteAsync(
            "POST",
            "/__contract/billing",
            $$"""
            {
              "items": [{
                "productId": "11111111-1111-1111-1111-111111111111",
                "quantity": "{{secret}}",
                "unitPrice": 1
              }]
            }
            """,
            "application/json");
        var json = response.Body.GetRawText();

        Assert.Equal(400, response.StatusCode);
        Assert.Contains("O valor informado é inválido.", json);
        Assert.DoesNotContain(secret, json);
        Assert.DoesNotContain("System.Nullable", json);
        Assert.DoesNotContain("BytePositionInLine", json);
    }

    [Theory]
    [InlineData("POST", "/__contract/billing", "text/plain", 415)]
    [InlineData("PUT", "/__contract/billing", null, 405)]
    [InlineData("GET", "/__contract/unknown", null, 404)]
    public async Task RoutingErrors_UseTheStandardEnvelope(
        string method,
        string path,
        string? contentType,
        int expectedStatus)
    {
        var response = await ExecuteAsync(method, path, "{}", contentType);

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.False(response.Body.GetProperty("success").GetBoolean());
        Assert.Equal(expectedStatus, response.Body.GetProperty("statusCode").GetInt32());
        Assert.Equal("billing-http-trace", response.Body.GetProperty("traceId").GetString());
        Assert.False(response.Body.TryGetProperty("detail", out _));
    }

    private static async Task<(int StatusCode, JsonElement Body)> ExecuteAsync(
        string method,
        string path,
        string body,
        string? contentType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var diagnosticListener = new DiagnosticListener("BillingContractTests");
        services.AddSingleton(diagnosticListener);
        services.AddSingleton<DiagnosticSource>(diagnosticListener);
        services
            .AddConfiguredApiControllers()
            .AddApplicationPart(typeof(BillingContractProbeController).Assembly);
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateInvoiceRequestValidator>();

        await using var provider = services.BuildServiceProvider();
        var application = new ApplicationBuilder(provider);
        application.UseRouting();
        application.UseApiErrorStatusCodePages();
        application.UseEndpoints(endpoints => endpoints.MapControllers());
        var pipeline = application.Build();

        await using var scope = provider.CreateAsyncScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            TraceIdentifier = "billing-http-trace"
        };
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.ContentType = contentType;
        context.Request.ContentLength = bodyBytes.Length;
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Response.Body = new MemoryStream();

        await pipeline(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }
}

[ApiController]
[Route("__contract/billing")]
public sealed class BillingContractProbeController : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    public IActionResult Post([FromBody] CreateInvoiceRequest request) => Ok(request);
}
