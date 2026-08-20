using InventoryService.Api.DTOs;

namespace InventoryService.Api.Services;

/// <summary>
/// Contrato do serviço de gerenciamento de produtos e estoque.
/// </summary>
public interface IProductService
{
    /// <summary>Retorna a lista paginada de todos os produtos.</summary>
    Task<IEnumerable<ProductResponse>> GetAllAsync(string? searchTerm, CancellationToken ct = default);

    /// <summary>Retorna os dados completos de um produto pelo seu Id.</summary>
    Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Retorna apenas o saldo atual de estoque de um produto pelo seu Id.</summary>
    Task<StockBalanceResponse> GetStockBalanceAsync(Guid id, CancellationToken ct = default);

    /// <summary>Cadastra um novo produto com saldo inicial de estoque.</summary>
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);

    /// <summary>Atualiza a descrição e unidade de um produto existente.</summary>
    Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default);

    /// <summary>
    /// Abate a quantidade informada do saldo do produto.
    /// Utiliza Optimistic Concurrency Control via xmin do PostgreSQL.
    /// </summary>
    Task<StockBalanceResponse> DeductStockAsync(Guid id, DeductStockRequest request, CancellationToken ct = default);

    /// <summary>Remove um produto do cadastro (somente se sem movimentações pendentes).</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
