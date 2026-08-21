namespace BillingService.Api.DTOs;

/// <summary>Payload para criação de um item dentro de uma nota fiscal.</summary>
public record CreateInvoiceItemRequest
{
    public Guid ProductId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}

/// <summary>Payload para criação de uma nova nota fiscal.</summary>
public record CreateInvoiceRequest
{
    public string? CustomerName { get; init; }

    public string? Notes { get; init; }

    public List<CreateInvoiceItemRequest> Items { get; init; } = new();
}
