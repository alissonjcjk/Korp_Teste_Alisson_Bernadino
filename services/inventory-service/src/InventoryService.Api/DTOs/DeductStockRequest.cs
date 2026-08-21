namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para abatimento de estoque de um produto.
/// Chamado internamente pelo BillingService no momento da impressão da NF.
/// </summary>
public record DeductStockRequest
{
    /// <summary>Quantidade a ser abatida do estoque.</summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// Referência da nota fiscal que originou o abatimento.
    /// Usada para rastreabilidade nos logs.
    /// </summary>
    public string InvoiceReference { get; init; } = string.Empty;
}
