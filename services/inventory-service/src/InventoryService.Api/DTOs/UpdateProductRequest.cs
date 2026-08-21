namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para atualização de um produto existente.
/// </summary>
public record UpdateProductRequest
{
    public string Description { get; init; } = string.Empty;

    public string Unit { get; init; } = "UN";
}
