using FluentValidation;
using InventoryService.Api.DTOs;

namespace InventoryService.Api.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A descrição do produto é obrigatória.")
            .MaximumLength(255).WithMessage("A descrição não pode ultrapassar 255 caracteres.");

        RuleFor(x => x.Unit)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A unidade é obrigatória.")
            .MaximumLength(20).WithMessage("A unidade não pode ultrapassar 20 caracteres.");
    }
}
