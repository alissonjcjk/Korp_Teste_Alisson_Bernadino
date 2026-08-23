using FluentValidation;
using BillingService.Api.DTOs;
using BillingService.Api.Models;

namespace BillingService.Api.Validators;

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public const int MaximumItems = 100;

    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .MaximumLength(255).WithMessage("O nome do cliente não pode ultrapassar 255 caracteres.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("As observações não podem ultrapassar 1000 caracteres.");

        RuleFor(x => x.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("A lista de itens é obrigatória.")
            .NotEmpty().WithMessage("A nota fiscal deve ter ao menos um item.")
            .Must(items => items!.Count <= MaximumItems)
                .WithMessage($"A nota fiscal não pode ter mais de {MaximumItems} itens.");

        RuleForEach(x => x.Items!)
            .NotNull().WithMessage("O item da nota fiscal não pode ser nulo.")
            .SetValidator(new CreateInvoiceItemRequestValidator())
            .When(x => x.Items is not null);

        RuleFor(x => x.Items)
            .Must(HasRepresentableInvoiceTotal)
            .When(x => x.Items is { Count: > 0 })
            .WithMessage("O valor total da nota fiscal não pode ultrapassar 14 dígitos inteiros e 4 casas decimais.");
    }

    private static bool HasRepresentableInvoiceTotal(
        List<CreateInvoiceItemRequest>? items)
    {
        if (items is null)
            return true;

        decimal total = 0;

        foreach (var item in items)
        {
            if (item is null || item.Quantity is not > 0 || item.UnitPrice is not >= 0)
                return true;

            if (item.Quantity > InvoiceAmount.MaxValue ||
                item.UnitPrice > InvoiceAmount.MaxValue)
            {
                return true;
            }

            var lineTotal = InvoiceAmount.CalculateLineTotal(
                item.Quantity.Value,
                item.UnitPrice.Value);

            // A regra do item informa esse caso no campo de preço unitário.
            if (lineTotal > InvoiceAmount.MaxValue)
                return true;

            if (total > InvoiceAmount.MaxValue - lineTotal)
                return false;

            total += lineTotal;
        }

        return true;
    }
}
