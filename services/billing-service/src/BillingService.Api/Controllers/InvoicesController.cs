using Microsoft.AspNetCore.Mvc;
using BillingService.Api.DTOs;
using BillingService.Api.Services;

namespace BillingService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InvoiceSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var invoices = await _invoiceService.GetAllAsync(ct);
        return Ok(ApiResponse<IEnumerable<InvoiceSummaryResponse>>.Ok(invoices));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await _invoiceService.CreateAsync(request, ct);

        return CreatedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = invoice.Id },
            value: ApiResponse<InvoiceResponse>.Ok(invoice, "Nota fiscal criada com sucesso."));
    }

    [HttpPost("{id:guid}/print")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Print(
        Guid id, 
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(ApiResponse<object>.Fail("O cabeçalho 'Idempotency-Key' é obrigatório."));
        }

        var invoice = await _invoiceService.PrintAsync(id, idempotencyKey, ct);
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice, "Nota fiscal impressa e fechada com sucesso."));
    }
}
