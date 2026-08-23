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
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var invoices = await _invoiceService.GetAllAsync(ct);
        return Ok(ApiResponse<IEnumerable<InvoiceSummaryResponse>>.Ok(invoices));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<InvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
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
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Print(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            const string message = "O cabeçalho 'Idempotency-Key' é obrigatório.";
            return BadRequest(ApiErrorResponseFactory.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                ApiErrorResponseFactory.ValidationMessage,
                HeaderErrors(message)));
        }

        if (idempotencyKey.Length > 100)
        {
            const string message = "O cabeçalho 'Idempotency-Key' não pode ultrapassar 100 caracteres.";
            return BadRequest(ApiErrorResponseFactory.Create(
                HttpContext,
                StatusCodes.Status400BadRequest,
                ApiErrorResponseFactory.ValidationMessage,
                HeaderErrors(message)));
        }

        var invoice = await _invoiceService.PrintAsync(id, idempotencyKey, ct);
        return Ok(ApiResponse<InvoiceResponse>.Ok(invoice, "Nota fiscal impressa e fechada com sucesso."));
    }

    private static IReadOnlyDictionary<string, string[]> HeaderErrors(string message) =>
        new Dictionary<string, string[]>
        {
            ["Idempotency-Key"] = new[] { message }
        };
}
