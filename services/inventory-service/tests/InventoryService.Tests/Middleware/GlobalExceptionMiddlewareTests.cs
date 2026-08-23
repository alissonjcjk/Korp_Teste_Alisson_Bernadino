using System.Text.Json;
using InventoryService.Api.Exceptions;
using InventoryService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace InventoryService.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithUnexpectedException_ReturnsOnlyGenericErrorContract()
    {
        var (context, json) = await InvokeAsync(
            new InvalidOperationException("segredo interno e stack trace"));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Equal(
            ["success", "statusCode", "message", "traceId", "timestamp"],
            root.EnumerateObject().Select(property => property.Name));
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal(500, root.GetProperty("statusCode").GetInt32());
        Assert.Equal(
            "Ocorreu um erro interno no servidor. Tente novamente mais tarde.",
            root.GetProperty("message").GetString());
        Assert.Equal("inventory-middleware-trace", root.GetProperty("traceId").GetString());
        Assert.True(root.GetProperty("timestamp").TryGetDateTimeOffset(out _));
        Assert.DoesNotContain("segredo interno", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("detail", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("duplicate", StatusCodes.Status409Conflict)]
    [InlineData("insufficient", StatusCodes.Status409Conflict)]
    [InlineData("not-found", StatusCodes.Status404NotFound)]
    public async Task InvokeAsync_WithDomainException_UsesDomainStatus(
        string exceptionKind,
        int expectedStatus)
    {
        Exception exception = exceptionKind switch
        {
            "duplicate" => new DuplicateProductCodeException("PROD-001"),
            "insufficient" => new InsufficientStockException("PROD-001", 2, 1),
            _ => new ProductNotFoundException(Guid.Parse(
                "11111111-1111-1111-1111-111111111111"))
        };

        var (context, json) = await InvokeAsync(exception);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus, document.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("inventory-middleware-trace", document.RootElement
            .GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithEfConcurrencyException_ReturnsConflict()
    {
        var (context, json) = await InvokeAsync(new DbUpdateConcurrencyException());

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Contains("Conflito de concorrência", json);
    }

    [Fact]
    public async Task InvokeAsync_WithPayloadTooLarge_Preserves413WithoutInternalDetails()
    {
        var exception = new BadHttpRequestException(
            "request-size-secret",
            StatusCodes.Status413PayloadTooLarge);

        var (context, json) = await InvokeAsync(exception);
        using var document = JsonDocument.Parse(json);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, context.Response.StatusCode);
        Assert.Equal(413, document.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("O corpo da requisição excede o tamanho permitido.",
            document.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain("request-size-secret", json);
    }

    [Fact]
    public async Task InvokeAsync_WithUniqueConstraintException_ReturnsConflictWithoutDatabaseDetail()
    {
        var exception = new DbUpdateException(
            "Falha ao salvar a entidade.",
            new PostgresException(
                "duplicate key violates unique constraint IX_products_code segredo-db",
                "ERROR",
                "ERROR",
                PostgresErrorCodes.UniqueViolation));

        var (context, json) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Contains("Já existe um produto cadastrado", json);
        Assert.DoesNotContain("IX_products_code", json);
        Assert.DoesNotContain("segredo-db", json);
    }

    [Fact]
    public async Task InvokeAsync_WithNonPostgresDuplicateText_ReturnsGenericServerError()
    {
        var exception = new DbUpdateException(
            "Falha ao salvar a entidade.",
            new InvalidOperationException("duplicate key violates unique constraint"));

        var (context, json) = await InvokeAsync(exception);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("Ocorreu um erro interno no servidor", json);
        Assert.DoesNotContain("duplicate key", json);
    }

    private static async Task<(DefaultHttpContext Context, string Json)> InvokeAsync(
        Exception exception)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "inventory-middleware-trace"
        };
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync(CancellationToken.None);
        return (context, json);
    }
}
