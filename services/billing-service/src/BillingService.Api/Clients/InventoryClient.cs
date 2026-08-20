using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingService.Api.Exceptions;

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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao consultar produto {ProductId} no InventoryService.", productId);
            throw new InventoryServiceUnavailableException(ex.Message);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao consultar produto {ProductId} no InventoryService.", productId);
            throw new InventoryServiceUnavailableException("Timeout na requisição.");
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException(ex.Message);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao consultar saldo do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException("Timeout na requisição.");
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
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Estoque insuficiente para produto {ProductId}. Resposta: {Body}",
                    productId, errorBody);
                throw new InventoryServiceUnavailableException(
                    $"Estoque insuficiente ou conflito de concorrência para o produto {productId}.");
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de rede ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException(ex.Message);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout ao abater estoque do produto {ProductId}.", productId);
            throw new InventoryServiceUnavailableException("Timeout na requisição de abatimento.");
        }
    }
}
