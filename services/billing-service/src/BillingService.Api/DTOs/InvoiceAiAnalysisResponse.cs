namespace BillingService.Api.DTOs;

/// <summary>Resultado consultivo da análise inteligente de uma nota fiscal.</summary>
public sealed record InvoiceAiAnalysisResponse
{
    public required bool IsAvailable { get; init; }
    public required bool HasAnomalies { get; init; }
    public required string RiskLevel { get; init; }
    public required string Summary { get; init; }
    public required IReadOnlyList<string> Risks { get; init; }
    public required IReadOnlyList<string> Suggestions { get; init; }
    public required string Provider { get; init; }
    public required DateTimeOffset AnalyzedAt { get; init; }

    public static InvoiceAiAnalysisResponse Unavailable() => new()
    {
        IsAvailable = false,
        HasAnomalies = false,
        RiskLevel = "unavailable",
        Summary = "A análise inteligente está temporariamente indisponível. A nota fiscal e a impressão continuam funcionando normalmente.",
        Risks = [],
        Suggestions = [],
        Provider = "Groq",
        AnalyzedAt = DateTimeOffset.UtcNow
    };
}
