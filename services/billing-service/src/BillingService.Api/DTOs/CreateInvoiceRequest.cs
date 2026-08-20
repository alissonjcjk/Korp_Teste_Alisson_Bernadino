using System.ComponentModel.DataAnnotations;

namespace BillingService.Api.DTOs;

/// <summary>Payload para criação de um item dentro de uma nota fiscal.</summary>
public record CreateInvoiceItemRequest
{
    [Required(ErrorMessage = "O Id do produto é obrigatório.")]
    public Guid ProductId { get; init; }

    [Required(ErrorMessage = "A quantidade é obrigatória.")]
    [Range(0.0001, double.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Quantity { get; init; }

    [Required(ErrorMessage = "O preço unitário é obrigatório.")]
    [Range(0, double.MaxValue, ErrorMessage = "O preço unitário não pode ser negativo.")]
    public decimal UnitPrice { get; init; }
}

/// <summary>Payload para criação de uma nova nota fiscal.</summary>
public record CreateInvoiceRequest
{
    [MaxLength(255, ErrorMessage = "O nome do cliente não pode ultrapassar 255 caracteres.")]
    public string? CustomerName { get; init; }

    [MaxLength(1000, ErrorMessage = "As observações não podem ultrapassar 1000 caracteres.")]
    public string? Notes { get; init; }

    [Required(ErrorMessage = "A nota fiscal deve ter ao menos um item.")]
    [MinLength(1, ErrorMessage = "A nota fiscal deve ter ao menos um item.")]
    public List<CreateInvoiceItemRequest> Items { get; init; } = new();
}
