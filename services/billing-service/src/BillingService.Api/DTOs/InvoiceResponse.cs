using BillingService.Api.Models;

namespace BillingService.Api.DTOs;

/// <summary>Resposta de um item de nota fiscal.</summary>
public record InvoiceItemResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductDescription { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>Resposta completa de uma nota fiscal.</summary>
public record InvoiceResponse
{
    public Guid Id { get; init; }
    public int InvoiceNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? Notes { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime? PrintedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<InvoiceItemResponse> Items { get; init; } = new();
}

/// <summary>Resposta resumida para listagem de notas fiscais.</summary>
public record InvoiceSummaryResponse
{
    public Guid Id { get; init; }
    public int InvoiceNumber { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Resposta genérica padronizada para todos os endpoints do BillingService.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Operação realizada com sucesso.")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message, Data = default };
}

/// <summary>
/// Extensões de mapeamento de Invoice → DTOs de resposta.
/// </summary>
public static class InvoiceMappingExtensions
{
    public static InvoiceResponse ToResponse(this Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Status = invoice.Status.ToString(),
        CustomerName = invoice.CustomerName,
        Notes = invoice.Notes,
        TotalAmount = invoice.TotalAmount,
        PrintedAt = invoice.PrintedAt,
        CreatedAt = invoice.CreatedAt,
        UpdatedAt = invoice.UpdatedAt,
        Items = invoice.Items.Select(i => new InvoiceItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductDescription = i.ProductDescription,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.Quantity * i.UnitPrice
        }).ToList()
    };

    public static InvoiceSummaryResponse ToSummary(this Invoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        Status = invoice.Status.ToString(),
        CustomerName = invoice.CustomerName,
        TotalAmount = invoice.TotalAmount,
        ItemCount = invoice.Items.Count,
        CreatedAt = invoice.CreatedAt
    };
}
