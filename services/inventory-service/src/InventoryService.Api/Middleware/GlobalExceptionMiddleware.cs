using System.Net;
using InventoryService.Api.DTOs;
using InventoryService.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventoryService.Api.Middleware;

/// <summary>
/// Middleware que intercepta todas as exceções não tratadas da pipeline
/// e retorna respostas JSON padronizadas com o código HTTP correto.
/// Evita que detalhes internos vazem para o cliente em qualquer ambiente.
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
                    "Unhandled exception after the response started. Path: {Path}",
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

            // ── Conflito de concorrência do EF Core ──────────────────────────
            DbUpdateConcurrencyException => (
                (int)HttpStatusCode.Conflict,
                "Conflito de concorrência detectado. Os dados foram modificados por outro processo. Tente novamente."
            ),

            // ── Violação de constraint única no banco (ex: código duplicado) ──
            DbUpdateException dbEx when IsUniqueConstraintViolation(dbEx) => (
                (int)HttpStatusCode.Conflict,
                "Já existe um produto cadastrado com o código informado."
            ),

            BadHttpRequestException badRequestException => (
                NormalizeClientErrorStatus(badRequestException.StatusCode),
                badRequestException.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "O corpo da requisição excede o tamanho permitido."
                    : "A requisição informada é inválida."
            ),

            // ── Qualquer outra exceção inesperada ─────────────────────────────
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno no servidor. Tente novamente mais tarde."
            )
        };

        // Log: erros 5xx como Error, 4xx como Warning
        if (statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception. Path: {Path}", context.Request.Path);
        else
            _logger.LogWarning(exception, "Domain exception. Path: {Path} | Message: {Message}",
                context.Request.Path, exception.Message);

        context.Response.StatusCode = statusCode;

        var response = ApiErrorResponseFactory.Create(context, statusCode, message);
        await context.Response.WriteAsJsonAsync(response, cancellationToken: context.RequestAborted);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
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

/// <summary>
/// Extension method para registrar o middleware de forma fluente no Program.cs.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
