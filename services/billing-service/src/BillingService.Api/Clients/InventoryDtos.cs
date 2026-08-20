namespace BillingService.Api.Clients;

/// <summary>
/// DTO retornado pelo InventoryService ao consultar o saldo de um produto.
/// Espelha o StockBalanceResponse do serviço remoto.
/// </summary>
public record InventoryStockBalanceDto
{
    public Guid ProductId { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal StockBalance { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// DTO retornado pelo InventoryService ao buscar dados completos de um produto.
/// </summary>
public record InventoryProductDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal StockBalance { get; init; }
    public string Unit { get; init; } = string.Empty;
}

/// <summary>
/// Envelope genérico da resposta do InventoryService (ApiResponse<T>).
/// </summary>
public record InventoryApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
}

/// <summary>
/// Payload enviado ao InventoryService para abater o estoque.
/// </summary>
public record DeductStockPayload
{
    public decimal Quantity { get; init; }
    public string InvoiceReference { get; init; } = string.Empty;
}
