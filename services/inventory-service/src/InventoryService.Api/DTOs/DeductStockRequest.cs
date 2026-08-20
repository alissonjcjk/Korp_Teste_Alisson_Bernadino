using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para abatimento de estoque de um produto.
/// Chamado internamente pelo BillingService no momento da impressão da NF.
/// </summary>
public record DeductStockRequest
{
    /// <summary>Quantidade a ser abatida do estoque.</summary>
    [Required(ErrorMessage = "A quantidade é obrigatória.")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Quantity { get; init; }

    /// <summary>
    /// Referência da nota fiscal que originou o abatimento.
    /// Usada para rastreabilidade nos logs.
    /// </summary>
    [Required(ErrorMessage = "O número da nota fiscal é obrigatório para rastreabilidade.")]
    public string InvoiceReference { get; init; } = string.Empty;
}
