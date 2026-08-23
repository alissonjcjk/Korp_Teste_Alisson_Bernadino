using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Data;
using InventoryService.Api.DTOs;
using InventoryService.Api.Exceptions;
using InventoryService.Api.Models;

namespace InventoryService.Api.Services;

/// <summary>
/// Implementação do serviço de produtos com EF Core e LINQ.
/// Todas as operações de escrita usam transações implícitas do EF Core.
/// O abatimento de estoque usa Optimistic Concurrency (xmin do PostgreSQL).
/// </summary>
public class ProductService : IProductService
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(InventoryDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Leitura ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<ProductResponse>> GetAllAsync(
        string? searchTerm, CancellationToken ct = default)
    {
        // LINQ: projeção direta no banco (sem carregar entidades completas)
        var query = _context.Products.AsNoTracking();

        // Filtro por termo de busca (código ou descrição) usando LINQ
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.Code.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        // Ordenação por código e projeção para DTO (evita over-fetching)
        return await query
            .OrderBy(p => p.Code)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                StockBalance = p.StockBalance,
                Unit = p.Unit,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // LINQ: FirstOrDefaultAsync com projeção
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                StockBalance = p.StockBalance,
                Unit = p.Unit,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (product is null)
            throw new ProductNotFoundException(id);

        return product;
    }

    public async Task<StockBalanceResponse> GetStockBalanceAsync(Guid id, CancellationToken ct = default)
    {
        // LINQ: projeção mínima (apenas campos de saldo)
        var balance = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new StockBalanceResponse
            {
                ProductId = p.Id,
                Code = p.Code,
                StockBalance = p.StockBalance,
                Unit = p.Unit,
                LastUpdated = p.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (balance is null)
            throw new ProductNotFoundException(id);

        return balance;
    }

    // ── Escrita ───────────────────────────────────────────────────────────────

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request, CancellationToken ct = default)
    {
        var code = request.Code!;
        var description = request.Description!;
        var stockBalance = request.StockBalance!.Value;
        var unit = request.Unit!;

        // Verifica duplicidade de código (LINQ Any)
        var codeExists = await _context.Products
            .AnyAsync(p => p.Code.ToLower() == code.ToLower(), ct);

        if (codeExists)
            throw new DuplicateProductCodeException(code);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpper(),
            Description = description.Trim(),
            StockBalance = stockBalance,
            Unit = unit.Trim().ToUpper(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Produto criado. Id: {Id} | Código: {Code} | Saldo inicial: {Balance}",
            product.Id, product.Code, product.StockBalance);

        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(
        Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var description = request.Description!;
        var unit = request.Unit!;

        // FindAsync: busca por PK, mais eficiente que FirstOrDefault
        var product = await _context.Products.FindAsync(new object[] { id }, ct);

        if (product is null)
            throw new ProductNotFoundException(id);

        product.Description = description.Trim();
        product.Unit = unit.Trim().ToUpper();
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Produto atualizado. Id: {Id} | Código: {Code}", product.Id, product.Code);

        return ToResponse(product);
    }

    public async Task<StockBalanceResponse> DeductStockAsync(
        Guid id, DeductStockRequest request, CancellationToken ct = default)
    {
        var quantity = request.Quantity!.Value;
        var invoiceReference = request.InvoiceReference!;

        // ATENÇÃO: Precisamos do tracking aqui (sem AsNoTracking)
        // para que o EF Core detecte o xmin e aplique OCC
        var product = await _context.Products.FindAsync(new object[] { id }, ct);

        if (product is null)
            throw new ProductNotFoundException(id);

        if (product.StockBalance < quantity)
            throw new InsufficientStockException(product.Code, quantity, product.StockBalance);

        product.StockBalance -= quantity;
        product.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // OCC: outro processo modificou o produto entre a leitura e a escrita
            throw new ConcurrencyConflictException(product.Code);
        }

        _logger.LogInformation(
            "Estoque abatido. Produto: {Code} | NF: {Invoice} | Quantidade: {Qty} | Saldo restante: {Balance}",
            product.Code, invoiceReference, quantity, product.StockBalance);

        return new StockBalanceResponse
        {
            ProductId = product.Id,
            Code = product.Code,
            StockBalance = product.StockBalance,
            Unit = product.Unit,
            LastUpdated = product.UpdatedAt
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, ct);

        if (product is null)
            throw new ProductNotFoundException(id);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Produto removido. Id: {Id} | Código: {Code}", id, product.Code);
    }

    // ── Mapeamento privado ────────────────────────────────────────────────────

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Description = p.Description,
        StockBalance = p.StockBalance,
        Unit = p.Unit,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
