using BillingService.Api.DTOs;

namespace BillingService.Api.Services;

public interface IInvoiceAiAnalyzer
{
    Task<InvoiceAiAnalysisResponse> AnalyzeAsync(
        InvoiceResponse invoice,
        CancellationToken cancellationToken = default);
}
