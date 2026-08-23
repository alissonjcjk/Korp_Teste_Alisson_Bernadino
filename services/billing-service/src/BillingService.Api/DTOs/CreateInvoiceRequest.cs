using BillingService.Api.OpenApi;

namespace BillingService.Api.DTOs;

/// <summary>Payload para criação de um item dentro de uma nota fiscal.</summary>
public record CreateInvoiceItemRequest
{
    [ApiSchemaRequired]
    public Guid ProductId { get; init; }

    [ApiSchemaRequired]
    public decimal? Quantity { get; init; }

    [ApiSchemaRequired]
    public decimal? UnitPrice { get; init; }
}

/// <summary>Payload para criação de uma nova nota fiscal.</summary>
public record CreateInvoiceRequest
{
    public string? CustomerName { get; init; }

    public string? Notes { get; init; }

    [ApiSchemaRequired]
    public List<CreateInvoiceItemRequest>? Items { get; init; }
}
