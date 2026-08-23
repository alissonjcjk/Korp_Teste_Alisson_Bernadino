using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingService.Api.Exceptions;
using Polly.CircuitBreaker;

namespace BillingService.Api.Clients;

/// <summary>
/// Implementação do cliente HTTP tipado para comunicação com o Inventory Service.
/// O HttpClient já vem pré-configurado com as políticas Polly (Retry + Circuit Breaker)
/// através da injeção de dependência registrada no Program.cs.
/// </summary>
public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InventoryClient(HttpClient httpClient, ILogger<InventoryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InventoryProductDto?> GetProductAsync(
        Guid productId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Consultando produto {ProductId} no InventoryService.", productId);

            var response = await _httpClient.GetAsync($"api/products/{productId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                .ReadFromJsonAsync<InventoryApiResponse<InventoryProductDto>>(JsonOptions, ct);

            return envelope?.Data;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker aberto ao consultar produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao consultar produto {ProductId} no InventoryService.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao consultar produto {ProductId} no InventoryService.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Resposta inválida ao consultar produto {ProductId} no InventoryService.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Formato de resposta não suportado ao consultar produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
    }

    public async Task<InventoryStockBalanceDto?> GetStockBalanceAsync(
        Guid productId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Consultando saldo do produto {ProductId} no InventoryService.", productId);

            var response = await _httpClient.GetAsync($"api/products/{productId}/stock-balance", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                .ReadFromJsonAsync<InventoryApiResponse<InventoryStockBalanceDto>>(JsonOptions, ct);

            return envelope?.Data;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker aberto ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Resposta inválida ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Formato de resposta não suportado ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
    }

    public async Task<InventoryStockBalanceDto?> DeductStockAsync(
        Guid productId, decimal quantity, string invoiceReference, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Abatendo estoque. Produto: {ProductId} | Quantidade: {Qty} | NF: {Invoice}",
                productId, quantity, invoiceReference);

            var payload = new DeductStockPayload
            {
                Quantity = quantity,
                InvoiceReference = invoiceReference
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"api/products/{productId}/deduct-stock", payload, ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            // 409 = estoque insuficiente ou conflito de concorrência
            if (response.StatusCode == HttpStatusCode.Conflict ||
                response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogWarning(
                    "Abatimento rejeitado pelo InventoryService. Produto: {ProductId} | Status: {StatusCode}",
                    productId, (int)response.StatusCode);

                var message = await ReadBusinessRejectionMessageAsync(response, ct);
                throw new InventoryOperationRejectedException(message, (int)response.StatusCode);
            }

            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                .ReadFromJsonAsync<InventoryApiResponse<InventoryStockBalanceDto>>(JsonOptions, ct);

            return envelope?.Data;
        }
        catch (InventoryServiceUnavailableException)
        {
            throw; // Relançar exceção de domínio sem encapsular
        }
        catch (InventoryOperationRejectedException)
        {
            throw;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker aberto ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de rede ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Resposta inválida ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError(ex, "Formato de resposta não suportado ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException();
        }
    }

    private static async Task<string> ReadBusinessRejectionMessageAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        try
        {
            var error = await response.Content
                .ReadFromJsonAsync<InventoryApiErrorResponse>(JsonOptions, ct);

            if (!string.IsNullOrWhiteSpace(error?.Message))
                return error.Message;
        }
        catch (JsonException)
        {
            // Corpo remoto inválido não deve mudar uma rejeição de negócio para erro técnico.
        }
        catch (NotSupportedException)
        {
            // Content-Type não suportado: usa mensagem segura abaixo.
        }

        return response.StatusCode == HttpStatusCode.Conflict
            ? "O abatimento de estoque foi rejeitado por conflito."
            : "A solicitação de abatimento de estoque foi rejeitada.";
    }
}
