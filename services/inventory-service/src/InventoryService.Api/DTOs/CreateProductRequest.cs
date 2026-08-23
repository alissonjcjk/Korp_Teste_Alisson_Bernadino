using InventoryService.Api.OpenApi;

namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para criação de um novo produto.
/// </summary>
public record CreateProductRequest
{
    /// <summary>Código único do produto (ex: "PROD-001").</summary>
    [ApiSchemaRequired]
    public string? Code { get; init; }

    /// <summary>Descrição detalhada do produto.</summary>
    [ApiSchemaRequired]
    public string? Description { get; init; }

    /// <summary>Saldo inicial de estoque. Deve ser maior ou igual a zero.</summary>
    [ApiSchemaRequired]
    public decimal? StockBalance { get; init; }

    /// <summary>Unidade de medida obrigatória.</summary>
    [ApiSchemaRequired]
    public string? Unit { get; init; }
}
