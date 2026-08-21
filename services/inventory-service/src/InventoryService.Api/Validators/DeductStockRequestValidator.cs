using FluentValidation;
using InventoryService.Api.DTOs;

namespace InventoryService.Api.Validators;

public class DeductStockRequestValidator : AbstractValidator<DeductStockRequest>
{
    public DeductStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .NotEmpty().WithMessage("A quantidade é obrigatória.")
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.InvoiceReference)
            .NotEmpty().WithMessage("O número da nota fiscal é obrigatório para rastreabilidade.");
    }
}
