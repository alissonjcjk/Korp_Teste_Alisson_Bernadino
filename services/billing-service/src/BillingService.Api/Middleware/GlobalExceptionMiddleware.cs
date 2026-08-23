using BillingService.Api.DTOs;
using BillingService.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly.CircuitBreaker;

namespace BillingService.Api.Middleware;

/// <summary>
/// Middleware global que intercepta todas as exceções não tratadas da pipeline
/// e retorna respostas JSON padronizadas com o código HTTP correto.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    ex,
                    "Não foi possível escrever o envelope de erro porque a resposta já foi iniciada. Path: {Path}",
                    context.Request.Path);
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            // ── Exceções de domínio (regras de negócio) ──────────────────────
            DomainException domainEx => (domainEx.StatusCode, domainEx.Message),

            // ── Circuit breaker aberto para o Inventory Service ──────────────
            BrokenCircuitException => (
                StatusCodes.Status503ServiceUnavailable,
                InventoryServiceUnavailableException.SafeMessage),

            // ── Timeout ou falha de rede com o Inventory Service ─────────────
            TaskCanceledException or HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                InventoryServiceUnavailableException.SafeMessage),

            BadHttpRequestException badRequestException => (
                NormalizeClientErrorStatus(badRequestException.StatusCode),
                badRequestException.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "O corpo da requisição excede o tamanho permitido."
                    : "A requisição informada é inválida."),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Conflito de concorrência detectado. Os dados foram modificados por outro processo. Tente novamente."),

            // ── Conflito de constraint única (idempotência duplicada) ─────────
            DbUpdateException dbEx when IsUniqueViolation(dbEx) => (
                StatusCodes.Status409Conflict,
                "Já existe um registro com os mesmos dados únicos."
            ),

            // ── Qualquer outra exceção não prevista ───────────────────────────
            _ => (
                StatusCodes.Status500InternalServerError,
                "Ocorreu um erro interno no servidor. Tente novamente mais tarde."
            )
        };

        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception. Path: {Path}", context.Request.Path);
        else
            _logger.LogWarning(exception, "Domain exception. Path: {Path} | Message: {Message}",
                context.Request.Path, exception.Message);

        context.Response.StatusCode = statusCode;

        var response = ApiErrorResponseFactory.Create(context, statusCode, message);
        await context.Response.WriteAsJsonAsync(
            response,
            cancellationToken: context.RequestAborted);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                })
            {
                return true;
            }
        }

        return false;
    }

    private static int NormalizeClientErrorStatus(int statusCode) =>
        statusCode is >= 400 and < 500
            ? statusCode
            : StatusCodes.Status400BadRequest;
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
