using FluentValidation;
using BillingService.Api.DTOs;

namespace BillingService.Api.Validators;

public class CreateInvoiceItemRequestValidator : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O Id do produto é obrigatório.");

        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage("A quantidade é obrigatória.")
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.UnitPrice)
            .NotNull().WithMessage("O preço unitário é obrigatório.")
            .GreaterThanOrEqualTo(0).WithMessage("O preço unitário não pode ser negativo.");
    }
}
