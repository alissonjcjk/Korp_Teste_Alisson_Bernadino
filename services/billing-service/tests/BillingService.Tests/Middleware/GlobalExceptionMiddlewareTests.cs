using System.Text.Json;
using BillingService.Api.Exceptions;
using BillingService.Api.Middleware;
using BillingService.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Polly.CircuitBreaker;

namespace BillingService.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_MapsInvoiceNotFoundTo404Envelope()
    {
        var response = await ExecuteAsync(new InvoiceNotFoundException(Guid.NewGuid()));

        Assert.Equal(404, response.StatusCode);
        Assert.Equal(404, response.Body.GetProperty("statusCode").GetInt32());
        Assert.Contains("não encontrada", response.Body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_MapsInvalidInvoiceStateTo409Envelope()
    {
        var response = await ExecuteAsync(new InvalidInvoiceStatusException(10, InvoiceStatus.Closed));

        Assert.Equal(409, response.StatusCode);
        Assert.Equal(409, response.Body.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_MapsMalformedRequestTo400Envelope()
    {
        var response = await ExecuteAsync(new BadHttpRequestException("transport-secret"));

        Assert.Equal(400, response.StatusCode);
        Assert.Equal("A requisição informada é inválida.",
            response.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("transport-secret", response.Body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_PreservesPayloadTooLargeStatusWithoutInternalDetails()
    {
        var response = await ExecuteAsync(new BadHttpRequestException(
            "request-size-secret",
            StatusCodes.Status413PayloadTooLarge));

        Assert.Equal(413, response.StatusCode);
        Assert.Equal(413, response.Body.GetProperty("statusCode").GetInt32());
        Assert.Equal("O corpo da requisição excede o tamanho permitido.",
            response.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("request-size-secret", response.Body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_MapsEfConcurrencyConflictTo409Envelope()
    {
        var response = await ExecuteAsync(new DbUpdateConcurrencyException("database-secret"));

        Assert.Equal(409, response.StatusCode);
        Assert.Contains("Conflito de concorrência",
            response.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("database-secret", response.Body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_MapsBrokenCircuitToSafe503WithoutInternalDetails()
    {
        var response = await ExecuteAsync(new BrokenCircuitException("circuit-secret"));
        var json = response.Body.GetRawText();

        Assert.Equal(503, response.StatusCode);
        Assert.Equal(InventoryServiceUnavailableException.SafeMessage,
            response.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("circuit-secret", json);
        Assert.False(response.Body.TryGetProperty("detail", out _));
        Assert.False(response.Body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task InvokeAsync_MapsUnexpectedExceptionToGeneric500ExactEnvelope()
    {
        var response = await ExecuteAsync(new InvalidOperationException("database-secret"));
        var properties = response.Body.EnumerateObject().Select(property => property.Name).ToArray();
        var json = response.Body.GetRawText();

        Assert.Equal(500, response.StatusCode);
        Assert.Equal("Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
            response.Body.GetProperty("message").GetString());
        Assert.Equal(new[] { "success", "statusCode", "message", "traceId", "timestamp" }, properties);
        Assert.Equal("trace-middleware", response.Body.GetProperty("traceId").GetString());
        Assert.DoesNotContain("database-secret", json);
    }

    [Fact]
    public async Task InvokeAsync_MapsPostgresUniqueViolationTo409WithoutDatabaseDetails()
    {
        var postgresException = new PostgresException(
            "duplicate key secret",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation);
        var response = await ExecuteAsync(new DbUpdateException(
            "save failed",
            new InvalidOperationException("wrapper", postgresException)));

        Assert.Equal(409, response.StatusCode);
        Assert.Equal("Já existe um registro com os mesmos dados únicos.",
            response.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("duplicate key secret", response.Body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_DoesNotInferUniqueViolationFromExceptionText()
    {
        var response = await ExecuteAsync(new DbUpdateException(
            "duplicate key violates unique constraint",
            new InvalidOperationException("unique-secret")));

        Assert.Equal(500, response.StatusCode);
        Assert.Equal("Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
            response.Body.GetProperty("message").GetString());
        Assert.DoesNotContain("unique-secret", response.Body.GetRawText());
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_RethrowsWithoutWritingEnvelope()
    {
        var responseFeature = new StartedResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.FromException(new InvalidOperationException("late failure")),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.True(context.Response.HasStarted);
        Assert.Equal(0, responseFeature.Body.Length);
    }

    private static async Task<(int StatusCode, JsonElement Body)> ExecuteAsync(Exception exception)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-middleware"
        };
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.FromException(exception),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
