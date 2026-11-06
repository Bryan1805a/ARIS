using Aris.Application.CivilStatuses.Dtos;

using FluentValidation;

namespace Aris.Application.CivilStatuses.Validators;

public sealed class CancelCivilStatusValidator : AbstractValidator<CancelCivilStatusDto> {
    public CancelCivilStatusValidator() {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("A cancellation reason is required.")
            .MaximumLength(300)
            .WithMessage("Cancellation reason cannot exceed 300 characters.");
    }
}