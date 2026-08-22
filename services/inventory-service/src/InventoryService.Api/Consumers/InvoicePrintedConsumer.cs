using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Data;
using InventoryService.Api.Exceptions;
using Korp.Shared.Events;

namespace InventoryService.Api.Consumers;

/// <summary>
/// Consome o evento InvoicePrintedEvent publicado pelo BillingService.
/// Realiza a dedução de estoque de cada item da nota fiscal de forma atômica.
///
/// Garantias arquiteturais:
///   - Idempotência: o MassTransit InboxState garante que a mesma mensagem
///     não seja processada mais de uma vez (evita dupla dedução de estoque).
///   - Atomicidade: todos os itens da NF são deduzidos em uma única transação
///     de banco. Se qualquer item falhar (ex: estoque insuficiente), o rollback
///     é automático e a mensagem é enviada para a dead-letter queue.
/// </summary>
public class InvoicePrintedConsumer : IConsumer<InvoicePrintedEvent>
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InvoicePrintedConsumer> _logger;

    public InvoicePrintedConsumer(InventoryDbContext context, ILogger<InvoicePrintedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvoicePrintedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Processando evento InvoicePrintedEvent para NF #{InvoiceNumber} (ID: {InvoiceId}). Itens: {ItemCount}.",
            message.InvoiceNumber, message.InvoiceId, message.Items.Count);

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Uma única transação para garantir que TODOS os itens são deduzidos ou NENHUM.
            await using var transaction = await _context.Database.BeginTransactionAsync(context.CancellationToken);

            try
            {
                foreach (var item in message.Items)
                {
                    // Busca o produto com tracking para que o EF Core detecte a mudança.
                    var product = await _context.Products.FindAsync(
                        new object[] { item.ProductId }, context.CancellationToken);

                    if (product is null)
                    {
                        _logger.LogError(
                            "Produto {ProductId} não encontrado ao processar NF #{InvoiceNumber}. Abortando transação.",
                            item.ProductId, message.InvoiceNumber);
                        throw new ProductNotFoundException(item.ProductId);
                    }

                    if (product.StockBalance < item.Quantity)
                    {
                        _logger.LogError(
                            "Estoque insuficiente para produto {Code} ao processar NF #{InvoiceNumber}. " +
                            "Necessário: {Qty}, Disponível: {Balance}. Abortando transação.",
                            product.Code, message.InvoiceNumber, item.Quantity, product.StockBalance);
                        throw new InsufficientStockException(product.Code, item.Quantity, product.StockBalance);
                    }

                    product.StockBalance -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation(
                        "Estoque deduzido. Produto: {Code} | NF: {InvoiceNumber} | Qty: {Qty} | Saldo restante: {Balance}",
                        product.Code, message.InvoiceNumber, item.Quantity, product.StockBalance);
                }

                await _context.SaveChangesAsync(context.CancellationToken);
                await transaction.CommitAsync(context.CancellationToken);

                _logger.LogInformation(
                    "Evento InvoicePrintedEvent para NF #{InvoiceNumber} processado com sucesso. {ItemCount} item(s) deduzido(s).",
                    message.InvoiceNumber, message.Items.Count);
            }
            catch
            {
                await transaction.RollbackAsync(context.CancellationToken);
                // Re-lança para que o MassTransit gerencie as retentativas e a dead-letter queue.
                throw;
            }
        });
    }
}
