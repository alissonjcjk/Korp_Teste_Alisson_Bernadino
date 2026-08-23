using System.Net;
using System.Text;
using BillingService.Api.Clients;
using BillingService.Api.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.CircuitBreaker;

namespace BillingService.Tests.Clients;

public class InventoryClientTests
{
    [Fact]
    public async Task GetProductAsync_WhenCircuitIsOpen_ReturnsSafeUnavailableException()
    {
        using var httpClient = HttpClientReturning(_ =>
            Task.FromException<HttpResponseMessage>(new BrokenCircuitException("circuit-secret")));
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<InventoryServiceUnavailableException>(() =>
            client.GetProductAsync(Guid.NewGuid()));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal(InventoryServiceUnavailableException.SafeMessage, exception.Message);
        Assert.DoesNotContain("circuit-secret", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_WhenNetworkFails_DoesNotExposeInternalMessage()
    {
        using var httpClient = HttpClientReturning(_ =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("dns-secret")));
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<InventoryServiceUnavailableException>(() =>
            client.GetStockBalanceAsync(Guid.NewGuid()));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal(InventoryServiceUnavailableException.SafeMessage, exception.Message);
        Assert.DoesNotContain("dns-secret", exception.Message);
    }

    [Fact]
    public async Task GetProductAsync_WhenSuccessBodyIsMalformed_MapsToSafeUnavailableException()
    {
        using var httpClient = HttpClientReturning(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("json-secret", Encoding.UTF8, "application/json")
        }));
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<InventoryServiceUnavailableException>(() =>
            client.GetProductAsync(Guid.NewGuid()));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal(InventoryServiceUnavailableException.SafeMessage, exception.Message);
        Assert.DoesNotContain("json-secret", exception.Message);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(409)]
    public async Task DeductStockAsync_WhenInventoryRejectsBusinessRule_PreservesRemoteStatus(int statusCode)
    {
        using var httpClient = HttpClientReturning(_ => Task.FromResult(new HttpResponseMessage(
            (HttpStatusCode)statusCode)
        {
            Content = JsonContent("Regra de estoque rejeitada.")
        }));
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<InventoryOperationRejectedException>(() =>
            client.DeductStockAsync(Guid.NewGuid(), 2m, "NF-10"));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal("Regra de estoque rejeitada.", exception.Message);
    }

    [Fact]
    public async Task DeductStockAsync_WhenRemoteErrorBodyIsInvalid_KeepsBusinessConflict()
    {
        using var httpClient = HttpClientReturning(_ => Task.FromResult(new HttpResponseMessage(
            HttpStatusCode.Conflict)
        {
            Content = new StringContent("internal html", Encoding.UTF8, "text/html")
        }));
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<InventoryOperationRejectedException>(() =>
            client.DeductStockAsync(Guid.NewGuid(), 2m, "NF-10"));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("O abatimento de estoque foi rejeitado por conflito.", exception.Message);
        Assert.DoesNotContain("internal html", exception.Message);
    }

    [Fact]
    public async Task DeductStockAsync_WhenRemoteReturns500_MapsToSafeUnavailableException()
    {
        using var httpClient = HttpClientReturning(_ => Task.FromResult(new HttpResponseMessage(
            HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("database-secret")
        }));
        var client = CreateClient(httpClient);

        var exception = await Assert.ThrowsAsync<InventoryServiceUnavailableException>(() =>
            client.DeductStockAsync(Guid.NewGuid(), 2m, "NF-10"));

        Assert.Equal(503, exception.StatusCode);
        Assert.Equal(InventoryServiceUnavailableException.SafeMessage, exception.Message);
        Assert.DoesNotContain("database-secret", exception.Message);
    }

    private static InventoryClient CreateClient(HttpClient httpClient) =>
        new(httpClient, NullLogger<InventoryClient>.Instance);

    private static HttpClient HttpClientReturning(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) =>
        new(new StubHttpMessageHandler((request, _) => responseFactory(request)))
        {
            BaseAddress = new Uri("http://inventory.test/")
        };

    private static StringContent JsonContent(string message) => new(
        $$"""
        {
          "success": false,
          "statusCode": 409,
          "message": "{{message}}",
          "traceId": "inventory-trace",
          "timestamp": "2026-08-22T12:00:00Z"
        }
        """,
        Encoding.UTF8,
        "application/json");

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request, cancellationToken);
    }
}
