using FluentValidation;
using InventoryService.Api.DTOs;

namespace InventoryService.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("O código do produto é obrigatório.")
            .MaximumLength(50).WithMessage("O código não pode ultrapassar 50 caracteres.")
            .Must(code => code!.All(character => !char.IsControl(character)))
                .WithMessage("O código do produto contém caracteres inválidos.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A descrição do produto é obrigatória.")
            .MaximumLength(255).WithMessage("A descrição não pode ultrapassar 255 caracteres.");

        RuleFor(x => x.StockBalance)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("O saldo inicial é obrigatório.")
            .GreaterThanOrEqualTo(0).WithMessage("O saldo inicial não pode ser negativo.")
            .PrecisionScale(18, 4, true)
                .WithMessage("O saldo inicial deve ter no máximo 14 dígitos inteiros e 4 casas decimais.");

        RuleFor(x => x.Unit)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A unidade é obrigatória.")
            .MaximumLength(20).WithMessage("A unidade não pode ultrapassar 20 caracteres.");
    }
}
