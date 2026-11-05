using Aris.Application.TemporaryResidences.Dtos;
using FluentValidation;

namespace Aris.Application.TemporaryResidences.Validators;

public sealed class UpdateTemporaryResidenceValidator : AbstractValidator<UpdateTemporaryResidenceDto>
{
    public UpdateTemporaryResidenceValidator()
    {
        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.");

        RuleFor(x => x.StayReason)
            .NotEmpty().WithMessage("Stay reason is required.")
            .MaximumLength(200).WithMessage("Stay reason cannot exceed 200 characters.");

        RuleFor(x => x.HostInfo)
            .MaximumLength(200).WithMessage("Host / landlord information cannot exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

