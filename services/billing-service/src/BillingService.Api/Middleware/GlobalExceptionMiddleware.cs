using System.Net;
using System.Text.Json;
using BillingService.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Api.Middleware;

/// <summary>
/// Middleware global que intercepta todas as exceções não tratadas da pipeline
/// e retorna respostas JSON padronizadas com o código HTTP correto.
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

            // ── Timeout do HttpClient (Inventory Service lento demais) ────────
            TaskCanceledException => (
                (int)HttpStatusCode.GatewayTimeout,
                "O Serviço de Estoque demorou demais para responder. Tente novamente."
            ),

            // ── HttpRequestException (falha de rede com Inventory) ────────────
            HttpRequestException => (
                (int)HttpStatusCode.ServiceUnavailable,
                "Não foi possível se comunicar com o Serviço de Estoque. Tente novamente mais tarde."
            ),

            // ── Conflito de constraint única (idempotência duplicada) ─────────
            DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("unique") == true => (
                (int)HttpStatusCode.Conflict,
                "Já existe um registro com os mesmos dados únicos."
            ),

            // ── Qualquer outra exceção não prevista ───────────────────────────
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno no servidor. Tente novamente mais tarde."
            )
        };

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

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
