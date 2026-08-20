namespace BillingService.Api.Clients;

/// <summary>
/// Contrato do cliente HTTP que se comunica com o Inventory Service.
/// Abstrair em interface permite injetar mocks em testes unitários.
/// </summary>
public interface IInventoryClient
{
    /// <summary>
    /// Busca os dados completos de um produto pelo seu Id.
    /// Usado no momento da criação da nota para validar e copiar dados do produto.
    /// </summary>
    Task<InventoryProductDto?> GetProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Consulta o saldo atual de estoque de um produto.
    /// </summary>
    Task<InventoryStockBalanceDto?> GetStockBalanceAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Abate a quantidade informada do estoque do produto.
    /// Chamado durante a impressão da nota fiscal.
    /// </summary>
    Task<InventoryStockBalanceDto?> DeductStockAsync(
        Guid productId, decimal quantity, string invoiceReference, CancellationToken ct = default);
}
