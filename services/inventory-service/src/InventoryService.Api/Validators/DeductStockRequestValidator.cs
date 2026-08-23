using FluentValidation;
using InventoryService.Api.DTOs;

namespace InventoryService.Api.Validators;

public class DeductStockRequestValidator : AbstractValidator<DeductStockRequest>
{
    public DeductStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("A quantidade é obrigatória.")
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.")
            .PrecisionScale(18, 4, true)
                .WithMessage("A quantidade deve ter no máximo 14 dígitos inteiros e 4 casas decimais.");

        RuleFor(x => x.InvoiceReference)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O número da nota fiscal é obrigatório para rastreabilidade.")
            .MaximumLength(100).WithMessage("A referência da nota fiscal não pode ultrapassar 100 caracteres.")
            .Must(reference => reference!.All(character => !char.IsControl(character)))
                .WithMessage("A referência da nota fiscal contém caracteres inválidos.");
    }
}
