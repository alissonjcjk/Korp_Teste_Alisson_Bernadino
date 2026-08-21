using FluentValidation;
using BillingService.Api.DTOs;

namespace BillingService.Api.Validators;

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .MaximumLength(255).WithMessage("O nome do cliente não pode ultrapassar 255 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("As observações não podem ultrapassar 1000 caracteres.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("A nota fiscal deve ter ao menos um item.")
            .Must(items => items != null && items.Count >= 1).WithMessage("A nota fiscal deve ter ao menos um item.");

        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemRequestValidator());
    }
}
