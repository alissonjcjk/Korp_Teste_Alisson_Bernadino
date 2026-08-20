using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para criação de um novo produto.
/// </summary>
public record CreateProductRequest
{
    /// <summary>Código único do produto (ex: "PROD-001").</summary>
    [Required(ErrorMessage = "O código do produto é obrigatório.")]
    [MaxLength(50, ErrorMessage = "O código não pode ultrapassar 50 caracteres.")]
    public string Code { get; init; } = string.Empty;

    /// <summary>Descrição detalhada do produto.</summary>
    [Required(ErrorMessage = "A descrição do produto é obrigatória.")]
    [MaxLength(255, ErrorMessage = "A descrição não pode ultrapassar 255 caracteres.")]
    public string Description { get; init; } = string.Empty;

    /// <summary>Saldo inicial de estoque. Deve ser maior ou igual a zero.</summary>
    [Required(ErrorMessage = "O saldo inicial é obrigatório.")]
    [Range(0, double.MaxValue, ErrorMessage = "O saldo inicial não pode ser negativo.")]
    public decimal StockBalance { get; init; }

    /// <summary>Unidade de medida (padrão: UN).</summary>
    [MaxLength(20, ErrorMessage = "A unidade não pode ultrapassar 20 caracteres.")]
    public string Unit { get; init; } = "UN";
}
