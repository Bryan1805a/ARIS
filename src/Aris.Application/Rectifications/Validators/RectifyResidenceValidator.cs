using Aris.Application.Rectifications.Dtos;

using FluentValidation;

namespace Aris.Application.Rectifications.Validators;

public sealed class RectifyResidenceValidator : AbstractValidator<RectifyResidenceDto> {
    public RectifyResidenceValidator() {
        // Check if ResidenceId is valid
        RuleFor(x => x.ResidenceId).GreaterThan(0).WithMessage("A residence is required.");

        // Check if AddressId is valid
        RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("An address is required.");

        // StartDate must not equal current date
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)).WithMessage("Start date is required.");

        // EndDate must not be empty and has to be greater or equal StartDate
        RuleFor(x => x.EndDate).Must((dto, endDate) => endDate is null || endDate.Value >= dto.StartDate).WithMessage("End date must be on or after the start date.");

        // Every action must have a Reason
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Explain why the row is being corrected.").MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
    }
}