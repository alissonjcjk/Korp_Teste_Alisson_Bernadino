using System.Net;
using System.Text.Json;
using InventoryService.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Api.Middleware;

/// <summary>
/// Middleware que intercepta todas as exceções não tratadas da pipeline
/// e retorna respostas JSON padronizadas com o código HTTP correto.
/// Evita que detalhes internos vazem para o cliente em produção.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
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
            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("unique") == true => (
                (int)HttpStatusCode.Conflict,
                "Já existe um registro com os mesmos dados únicos (código duplicado)."
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

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            success = false,
            statusCode,
            message,
            // Detalhes do stack trace apenas em ambiente de desenvolvimento
            detail = _env.IsDevelopment() ? exception.ToString() : null,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method para registrar o middleware de forma fluente no Program.cs.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
