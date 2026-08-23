using System.Reflection;
using BillingService.Api.Controllers;
using BillingService.Api.DTOs;
using BillingService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BillingService.Tests.Controllers;

public class InvoicesControllerTests
{
    [Fact]
    public void Actions_DescribeEveryErrorResponseWithTheStandardEnvelope()
    {
        var errorResponses = typeof(InvoicesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<ProducesResponseTypeAttribute>())
            .Where(attribute => attribute.StatusCode >= StatusCodes.Status400BadRequest)
            .ToArray();

        Assert.NotEmpty(errorResponses);
        Assert.All(errorResponses, response =>
            Assert.Equal(typeof(ApiErrorResponse), response.Type));
    }

    [Fact]
    public void Create_ConsumesJsonAndDescribesUnsupportedMediaType()
    {
        var action = typeof(InvoicesController).GetMethod(nameof(InvoicesController.Create))!;
        var consumes = action.GetCustomAttribute<ConsumesAttribute>();
        var unsupportedMediaType = action
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .SingleOrDefault(attribute =>
                attribute.StatusCode == StatusCodes.Status415UnsupportedMediaType);

        Assert.NotNull(consumes);
        Assert.Contains("application/json", consumes.ContentTypes);
        Assert.NotNull(unsupportedMediaType);
        Assert.Equal(typeof(ApiErrorResponse), unsupportedMediaType.Type);
    }

    [Fact]
    public async Task Print_WhenIdempotencyKeyIsMissing_ReturnsStandardValidationEnvelope()
    {
        var service = new Mock<IInvoiceService>();
        var controller = CreateController(service, "trace-header-required");

        var result = await controller.Print(Guid.NewGuid(), null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ApiErrorResponse>(badRequest.Value);
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("trace-header-required", error.TraceId);
        Assert.Equal(ApiErrorResponseFactory.ValidationMessage, error.Message);
        Assert.Contains("Idempotency-Key", error.Errors!.Keys);
        Assert.Contains("obrigatório", error.Errors["Idempotency-Key"].Single());
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Print_WhenIdempotencyKeyExceeds100Characters_ReturnsStandardValidationEnvelope()
    {
        var service = new Mock<IInvoiceService>();
        var controller = CreateController(service, "trace-header-length");

        var result = await controller.Print(
            Guid.NewGuid(),
            new string('k', 101),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var error = Assert.IsType<ApiErrorResponse>(badRequest.Value);
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("trace-header-length", error.TraceId);
        Assert.Equal(ApiErrorResponseFactory.ValidationMessage, error.Message);
        Assert.Contains("Idempotency-Key", error.Errors!.Keys);
        Assert.Contains("100", error.Errors["Idempotency-Key"].Single());
        service.VerifyNoOtherCalls();
    }

    private static InvoicesController CreateController(
        Mock<IInvoiceService> service,
        string traceIdentifier)
    {
        var controller = new InvoicesController(
            service.Object,
            NullLogger<InvoicesController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = traceIdentifier
                }
            }
        };

        return controller;
    }
}
