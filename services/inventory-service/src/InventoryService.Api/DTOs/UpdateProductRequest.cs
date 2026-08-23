using InventoryService.Api.OpenApi;

namespace InventoryService.Api.DTOs;

/// <summary>
/// Payload para atualização de um produto existente.
/// </summary>
public record UpdateProductRequest
{
    [ApiSchemaRequired]
    public string? Description { get; init; }

    [ApiSchemaRequired]
    public string? Unit { get; init; }
}
