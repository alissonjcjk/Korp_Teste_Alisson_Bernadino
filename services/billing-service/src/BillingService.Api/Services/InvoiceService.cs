using Microsoft.EntityFrameworkCore;
using BillingService.Api.Data;
using BillingService.Api.DTOs;
using BillingService.Api.Exceptions;
using BillingService.Api.Models;
using BillingService.Api.Clients;

namespace BillingService.Api.Services;

public class InvoiceService : IInvoiceService
{
    private readonly BillingDbContext _context;
    private readonly IInventoryClient _inventoryClient;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        BillingDbContext context, 
        IInventoryClient inventoryClient,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _inventoryClient = inventoryClient;
        _logger = logger;
    }

    public async Task<IEnumerable<InvoiceSummaryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ToSummary())
            .ToListAsync(ct);
    }

    public async Task<InvoiceResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice is null)
            throw new InvoiceNotFoundException(id);

        return invoice.ToResponse();
    }

    public async Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        if (request.Items == null || !request.Items.Any())
            throw new InvoiceHasNoItemsException();

        // Gera número sequencial da NF
        var nextNumber = await _context.Invoices
            .AnyAsync(ct)
            ? await _context.Invoices.MaxAsync(i => i.InvoiceNumber, ct) + 1
            : 1;

        var invoice = new Invoice
        {
            InvoiceNumber = nextNumber,
            CustomerName = request.CustomerName?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = InvoiceStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;

        foreach (var reqItem in request.Items)
        {
            // Valida o produto chamando o serviço de estoque de forma síncrona/esperada
            var product = await _inventoryClient.GetProductAsync(reqItem.ProductId, ct);
            if (product == null)
            {
                throw new DomainException($"Produto com ID {reqItem.ProductId} não encontrado no serviço de estoque.", 400);
            }

            var invoiceItem = new InvoiceItem
            {
                ProductId = reqItem.ProductId,
                ProductCode = product.Code,
                ProductDescription = product.Description,
                Quantity = reqItem.Quantity,
                UnitPrice = reqItem.UnitPrice
            };

            totalAmount += invoiceItem.TotalPrice;
            invoice.Items.Add(invoiceItem);
        }

        invoice.TotalAmount = totalAmount;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Nota Fiscal criada. ID: {InvoiceId}, Numero: {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);

        return invoice.ToResponse();
    }

    public async Task<InvoiceResponse> PrintAsync(Guid id, string idempotencyKey, CancellationToken ct = default)
    {
        // Verificar se a chave de idempotência já existe. Se existir, retornar erro (ou a nota já fechada).
        var existingInvoiceWithKey = await _context.Invoices
            .FirstOrDefaultAsync(i => i.IdempotencyKey == idempotencyKey, ct);
        
        if (existingInvoiceWithKey != null && existingInvoiceWithKey.Id != id)
        {
            throw new DuplicateIdempotencyKeyException(idempotencyKey);
        }

        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            throw new InvoiceNotFoundException(id);

        // Regra de negócio: somente notas Abertas podem ser impressas
        if (invoice.Status != InvoiceStatus.Open)
        {
            // Se já foi impressa por ESSA idempotencyKey, podemos só retornar (sucesso idempotente)
            if (invoice.IdempotencyKey == idempotencyKey && invoice.Status == InvoiceStatus.Closed)
            {
                return invoice.ToResponse();
            }
            throw new InvalidInvoiceStatusException(invoice.InvoiceNumber, invoice.Status);
        }

        // Para cada item, tenta deduzir do estoque
        foreach (var item in invoice.Items)
        {
            var stockResult = await _inventoryClient.DeductStockAsync(item.ProductId, item.Quantity, invoice.InvoiceNumber.ToString(), ct);
            if (stockResult == null)
            {
                throw new DomainException($"Não foi possível deduzir estoque do produto {item.ProductId}.", 400);
            }
        }

        invoice.Status = InvoiceStatus.Closed;
        invoice.PrintedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.IdempotencyKey = idempotencyKey;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true)
        {
             throw new DuplicateIdempotencyKeyException(idempotencyKey);
        }

        _logger.LogInformation("Nota Fiscal impressa e fechada. ID: {InvoiceId}, Numero: {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber);

        return invoice.ToResponse();
    }
}
