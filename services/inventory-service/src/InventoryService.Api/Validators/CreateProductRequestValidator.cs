using FluentValidation;
using InventoryService.Api.DTOs;

namespace InventoryService.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("O código do produto é obrigatório.")
            .MaximumLength(50).WithMessage("O código não pode ultrapassar 50 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição do produto é obrigatória.")
            .MaximumLength(255).WithMessage("A descrição não pode ultrapassar 255 caracteres.");

        RuleFor(x => x.StockBalance)
            .NotNull().WithMessage("O saldo inicial é obrigatório.")
            .GreaterThanOrEqualTo(0).WithMessage("O saldo inicial não pode ser negativo.");

        RuleFor(x => x.Unit)
            .MaximumLength(20).WithMessage("A unidade não pode ultrapassar 20 caracteres.");
    }
}
