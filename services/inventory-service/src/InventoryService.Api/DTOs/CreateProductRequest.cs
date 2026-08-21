namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para criação de um novo produto.
/// </summary>
public record CreateProductRequest
{
    /// <summary>Código único do produto (ex: "PROD-001").</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Descrição detalhada do produto.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Saldo inicial de estoque. Deve ser maior ou igual a zero.</summary>
    public decimal StockBalance { get; init; }

    /// <summary>Unidade de medida (padrão: UN).</summary>
    public string Unit { get; init; } = "UN";
}
