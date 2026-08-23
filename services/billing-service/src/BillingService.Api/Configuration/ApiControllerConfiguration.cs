using BillingService.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Api.Configuration;

public static class ApiControllerConfiguration
{
    private const string InvalidValueMessage = "O valor informado é inválido.";

    public static IMvcBuilder AddConfiguredApiControllers(this IServiceCollection services)
    {
        return services
            .AddControllers(options =>
            {
                var messages = options.ModelBindingMessageProvider;
                messages.SetMissingRequestBodyRequiredValueAccessor(
                    () => "O corpo da requisição é obrigatório.");
                messages.SetAttemptedValueIsInvalidAccessor((_, _) => InvalidValueMessage);
                messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => InvalidValueMessage);
                messages.SetUnknownValueIsInvalidAccessor(_ => InvalidValueMessage);
                messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => InvalidValueMessage);
                messages.SetValueIsInvalidAccessor(_ => InvalidValueMessage);
                messages.SetValueMustBeANumberAccessor(_ => InvalidValueMessage);
                messages.SetNonPropertyValueMustBeANumberAccessor(() => InvalidValueMessage);
            })
            .AddJsonOptions(options =>
            {
                options.AllowInputFormatterExceptionMessages = false;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    var error = ApiErrorResponseFactory.FromModelState(
                        actionContext.HttpContext,
                        actionContext.ModelState);

                    return new BadRequestObjectResult(error);
                };
            });
    }
}
