namespace BillingService.Api.Models;

/// <summary>Regras numéricas compartilhadas pelos cálculos e validadores da nota fiscal.</summary>
public static class InvoiceAmount
{
    public const int Scale = 4;
    public const decimal MaxValue = 99_999_999_999_999.9999m;

    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice) =>
        decimal.Round(quantity * unitPrice, Scale, MidpointRounding.AwayFromZero);
}
