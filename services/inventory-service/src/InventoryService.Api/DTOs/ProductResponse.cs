namespace InventoryService.Api.DTOs;

/// <summary>
/// Resposta padronizada com os dados de um produto.
/// </summary>
public record ProductResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal StockBalance { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Resposta resumida apenas com o saldo atual do produto.
/// </summary>
public record StockBalanceResponse
{
    public Guid ProductId { get; init; }
    public string Code { get; init; } = string.Empty;
    public decimal StockBalance { get; init; }
    public string Unit { get; init; } = string.Empty;
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Resposta genérica para operações bem-sucedidas.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Operação realizada com sucesso.")
        => new() { Success = true, Message = message, Data = data };

}
