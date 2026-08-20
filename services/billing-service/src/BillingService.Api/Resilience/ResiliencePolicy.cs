using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace BillingService.Api.Resilience;

/// <summary>
/// Define as políticas de resiliência Polly aplicadas ao HttpClient
/// que comunica com o Serviço de Estoque.
///
/// Padrões implementados:
///   1. Retry Policy: tenta novamente em caso de falha transitória.
///   2. Circuit Breaker: abre o circuito após múltiplas falhas consecutivas,
///      evitando sobrecarga de um serviço já em falha.
/// </summary>
public static class ResiliencePolicy
{
    /// <summary>
    /// Política de Retry exponencial com jitter.
    ///
    /// - Tenta até 3 vezes após a falha inicial.
    /// - Aguarda 2^tentativa segundos + delay aleatório (jitter) entre tentativas.
    ///   Ex: 1ª retry ≈ 2s, 2ª ≈ 4s, 3ª ≈ 8s (com variação aleatória).
    /// - Aplica somente em erros HTTP 5xx e de rede (transientes).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        var jitter = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError()          // 5xx, 408, HttpRequestException
            .OrResult(msg =>
                msg.StatusCode == HttpStatusCode.TooManyRequests)  // 429
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    + TimeSpan.FromMilliseconds(jitter.Next(0, 300)),
                onRetry: (outcome, timeSpan, retryAttempt, context) =>
                {
                    // Log da tentativa de retry (acessível via ILogger no contexto)
                    Console.WriteLine(
                        $"[RETRY] Tentativa {retryAttempt} após falha: " +
                        $"{outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}. " +
                        $"Aguardando {timeSpan.TotalSeconds:F2}s...");
                });
    }

    /// <summary>
    /// Política de Circuit Breaker.
    ///
    /// - Abre o circuito após 5 falhas consecutivas.
    /// - Mantém o circuito ABERTO por 30 segundos (nenhuma requisição passa).
    /// - Após 30s, permite uma requisição de teste (estado HALF-OPEN).
    ///   Se bem-sucedida, fecha o circuito novamente.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    Console.WriteLine(
                        $"[CIRCUIT BREAKER] Circuito ABERTO por {breakDelay.TotalSeconds}s. " +
                        $"Motivo: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.WriteLine("[CIRCUIT BREAKER] Circuito FECHADO. Serviço de Estoque recuperado.");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("[CIRCUIT BREAKER] Circuito HALF-OPEN. Testando disponibilidade...");
                });
    }
}
