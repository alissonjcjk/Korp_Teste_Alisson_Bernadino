using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BillingService.Api.DTOs;

/// <summary>Envelope único para todas as respostas de erro da API.</summary>
public sealed record ApiErrorResponse
{
    public bool Success => false;
    public required int StatusCode { get; init; }
    public required string Message { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public required string TraceId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>Cria o envelope de erro usado pelo middleware, MVC e controllers.</summary>
public static class ApiErrorResponseFactory
{
    private const string InputFormatterErrorMessage =
        "An error occurred while deserializing input data.";

    public const string ValidationMessage = "Um ou mais erros de validação ocorreram.";

    public static ApiErrorResponse Create(
        HttpContext context,
        int statusCode,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new ApiErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Errors = errors is { Count: > 0 } ? errors : null,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public static ApiErrorResponse FromModelState(
        HttpContext context,
        ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(modelState);

        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => error.Exception is not null ||
                                     string.Equals(
                                         error.ErrorMessage,
                                         InputFormatterErrorMessage,
                                         StringComparison.Ordinal)
                        ? "O valor informado é inválido."
                        : string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "O valor informado é inválido."
                            : error.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray());

        return Create(
            context,
            StatusCodes.Status400BadRequest,
            ValidationMessage,
            errors);
    }

    public static ApiErrorResponse FromStatusCode(HttpContext context, int statusCode)
    {
        var message = statusCode switch
        {
            StatusCodes.Status404NotFound => "O recurso solicitado não foi encontrado.",
            StatusCodes.Status405MethodNotAllowed =>
                "O método HTTP informado não é permitido para este recurso.",
            StatusCodes.Status415UnsupportedMediaType =>
                "O formato do conteúdo enviado não é suportado.",
            _ => "A requisição não pôde ser processada."
        };

        return Create(context, statusCode, message);
    }
}
