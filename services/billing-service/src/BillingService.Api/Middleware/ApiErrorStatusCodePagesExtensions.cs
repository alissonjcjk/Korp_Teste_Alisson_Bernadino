using BillingService.Api.DTOs;

namespace BillingService.Api.Middleware;

public static class ApiErrorStatusCodePagesExtensions
{
    /// <summary>
    /// Padroniza erros produzidos pela pipeline sem corpo, como rota inexistente,
    /// método não permitido e tipo de mídia não suportado.
    /// </summary>
    public static IApplicationBuilder UseApiErrorStatusCodePages(this IApplicationBuilder app)
    {
        return app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;
            if (httpContext.Response.StatusCode is not (
                StatusCodes.Status404NotFound or
                StatusCodes.Status405MethodNotAllowed or
                StatusCodes.Status415UnsupportedMediaType))
            {
                return;
            }

            var error = ApiErrorResponseFactory.FromStatusCode(
                httpContext,
                httpContext.Response.StatusCode);

            await httpContext.Response.WriteAsJsonAsync(
                error,
                cancellationToken: httpContext.RequestAborted);
        });
    }
}
