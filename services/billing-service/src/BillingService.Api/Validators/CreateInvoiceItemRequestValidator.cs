using FluentValidation;
using BillingService.Api.DTOs;
using BillingService.Api.Models;

namespace BillingService.Api.Validators;

public class CreateInvoiceItemRequestValidator : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("O Id do produto é obrigatório.");

        RuleFor(x => x.Quantity)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("A quantidade é obrigatória.")
            .PrecisionScale(18, 4, ignoreTrailingZeros: true)
                .WithMessage("A quantidade deve ter no máximo 14 dígitos inteiros e 4 casas decimais.")
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.UnitPrice)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("O preço unitário é obrigatório.")
            .PrecisionScale(18, 4, ignoreTrailingZeros: true)
                .WithMessage("O preço unitário deve ter no máximo 14 dígitos inteiros e 4 casas decimais.")
            .GreaterThanOrEqualTo(0).WithMessage("O preço unitário não pode ser negativo.")
            .Must((request, _) => HasRepresentableLineTotal(request))
                .WithMessage("O total do item não pode ultrapassar 14 dígitos inteiros e 4 casas decimais.");
    }

    private static bool HasRepresentableLineTotal(CreateInvoiceItemRequest request)
    {
        if (request.Quantity is not > 0 || request.UnitPrice is not >= 0)
            return true;

        if (request.Quantity > InvoiceAmount.MaxValue ||
            request.UnitPrice > InvoiceAmount.MaxValue)
        {
            return true;
        }

        return InvoiceAmount.CalculateLineTotal(
            request.Quantity.Value,
            request.UnitPrice.Value) <= InvoiceAmount.MaxValue;
    }
}
