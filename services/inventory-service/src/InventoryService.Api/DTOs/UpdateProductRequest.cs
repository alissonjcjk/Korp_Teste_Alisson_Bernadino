using System.ComponentModel.DataAnnotations;

namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para atualização de um produto existente.
/// </summary>
public record UpdateProductRequest
{
    [Required(ErrorMessage = "A descrição do produto é obrigatória.")]
    [MaxLength(255, ErrorMessage = "A descrição não pode ultrapassar 255 caracteres.")]
    public string Description { get; init; } = string.Empty;

    [MaxLength(20, ErrorMessage = "A unidade não pode ultrapassar 20 caracteres.")]
    public string Unit { get; init; } = "UN";
}
