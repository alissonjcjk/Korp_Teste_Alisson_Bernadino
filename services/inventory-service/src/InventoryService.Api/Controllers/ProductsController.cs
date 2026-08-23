using Microsoft.AspNetCore.Mvc;
using InventoryService.Api.DTOs;
using InventoryService.Api.Services;

namespace InventoryService.Api.Controllers;

/// <summary>
/// Controller responsável pelo CRUD de produtos e operações de estoque.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>Lista todos os produtos. Aceita filtro opcional por código ou descrição.</summary>
    /// <param name="search">Termo de busca (opcional).</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var products = await _productService.GetAllAsync(search, ct);
        return Ok(ApiResponse<IEnumerable<ProductResponse>>.Ok(products));
    }

    /// <summary>Retorna os dados completos de um produto pelo Id.</summary>
    /// <param name="id">Identificador único do produto.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ProductResponse>.Ok(product));
    }

    /// <summary>
    /// Retorna apenas o saldo atual de estoque do produto.
    /// Endpoint chamado pelo BillingService antes da impressão da NF.
    /// </summary>
    /// <param name="id">Identificador único do produto.</param>
    [HttpGet("{id:guid}/stock-balance")]
    [ProducesResponseType(typeof(ApiResponse<StockBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockBalance(Guid id, CancellationToken ct)
    {
        var balance = await _productService.GetStockBalanceAsync(id, ct);
        return Ok(ApiResponse<StockBalanceResponse>.Ok(balance));
    }

    /// <summary>Cadastra um novo produto com saldo inicial de estoque.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.CreateAsync(request, ct);

        return CreatedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = product.Id },
            value: ApiResponse<ProductResponse>.Ok(product, "Produto cadastrado com sucesso."));
    }

    /// <summary>Atualiza a descrição e unidade de um produto existente.</summary>
    /// <param name="id">Identificador único do produto.</param>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var product = await _productService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<ProductResponse>.Ok(product, "Produto atualizado com sucesso."));
    }

    /// <summary>
    /// Abate a quantidade informada do saldo de estoque do produto.
    /// Utiliza Optimistic Concurrency Control para garantir consistência
    /// em cenários de acesso simultâneo (ex: dois faturamentos ao mesmo tempo).
    /// </summary>
    /// <param name="id">Identificador único do produto.</param>
    [HttpPost("{id:guid}/deduct-stock")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<StockBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeductStock(
        Guid id,
        [FromBody] DeductStockRequest request,
        CancellationToken ct)
    {
        var balance = await _productService.DeductStockAsync(id, request, ct);
        return Ok(ApiResponse<StockBalanceResponse>.Ok(balance,
            $"Estoque abatido com sucesso. Saldo atual: {balance.StockBalance} {balance.Unit}."));
    }

    /// <summary>Remove um produto do cadastro.</summary>
    /// <param name="id">Identificador único do produto.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
