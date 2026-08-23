using System.Text.Json;
using BillingService.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BillingService.Tests.Middleware;

public class ApiErrorStatusCodePagesTests
{
    [Theory]
    [InlineData(404, "O recurso solicitado não foi encontrado.")]
    [InlineData(405, "O método HTTP informado não é permitido para este recurso.")]
    [InlineData(415, "O formato do conteúdo enviado não é suportado.")]
    public async Task PipelineStatusWithoutBody_UsesStandardErrorEnvelope(
        int statusCode,
        string expectedMessage)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var application = new ApplicationBuilder(services);
        application.UseApiErrorStatusCodePages();
        application.Run(context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        });
        var pipeline = application.Build();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            TraceIdentifier = $"trace-{statusCode}"
        };
        context.Response.Body = new MemoryStream();

        await pipeline(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var body = document.RootElement;
        Assert.Equal(statusCode, context.Response.StatusCode);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal(statusCode, body.GetProperty("statusCode").GetInt32());
        Assert.Equal(expectedMessage, body.GetProperty("message").GetString());
        Assert.Equal($"trace-{statusCode}", body.GetProperty("traceId").GetString());
        Assert.False(body.TryGetProperty("errors", out _));
        Assert.False(body.TryGetProperty("detail", out _));
    }
}
