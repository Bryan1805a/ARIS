using Aris.Application.TemporaryResidences.Dtos;
using FluentValidation;

namespace Aris.Application.TemporaryResidences.Validators;

public sealed class RegisterTemporaryResidenceValidator : AbstractValidator<RegisterTemporaryResidenceDto>
{
    public RegisterTemporaryResidenceValidator()
    {
        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches("^[0-9]{12}$").WithMessage("National ID must be exactly 12 digits.");

        RuleFor(x => x.AddressId)
            .GreaterThan(0).WithMessage("Temporary residence address must be selected.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .Must((dto, endDate) => endDate >= dto.StartDate)
            .WithMessage("End date must be greater than or equal to start date.")
            .Must((dto, endDate) => endDate.DayNumber - dto.StartDate.DayNumber <= 731)
            .WithMessage("Temporary residence duration cannot exceed 2 years (730 days) by law.");

        RuleFor(x => x.StayReason)
            .NotEmpty().WithMessage("Stay reason is required.")
            .MaximumLength(200).WithMessage("Stay reason cannot exceed 200 characters.");

        RuleFor(x => x.HostInfo)
            .MaximumLength(200).WithMessage("Host / landlord information cannot exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

