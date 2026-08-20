using BillingService.Api.DTOs;

namespace BillingService.Api.Services;

/// <summary>
/// Contrato do serviço de notas fiscais.
/// </summary>
public interface IInvoiceService
{
    /// <summary>Lista todas as notas fiscais resumidas.</summary>
    Task<IEnumerable<InvoiceSummaryResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Retorna uma nota fiscal detalhada pelo Id.</summary>
    Task<InvoiceResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cria uma nova nota fiscal (com status inicial Aberta).</summary>
    Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Imprime a nota fiscal. Esta operação bate no microsserviço de Estoque
    /// para deduzir o saldo, atualiza o status da NF para Fechada e implementa
    /// idempotência para evitar duplicação.
    /// </summary>
    Task<InvoiceResponse> PrintAsync(Guid id, string idempotencyKey, CancellationToken ct = default);
}
