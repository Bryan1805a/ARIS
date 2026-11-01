using Aris.Application.Rectifications.Dtos;

using FluentValidation;

namespace Aris.Application.Rectifications.Validators;

public sealed class RectifyHouseholdAddressValidator : AbstractValidator<RectifyHouseholdAddressDto> {
    public RectifyHouseholdAddressValidator() {
        // Check if HouseholdAddressId is exists
        RuleFor(x => x.HouseholdAddressId).GreaterThan(0).WithMessage("A household address is required.");

        // Check if AddressId is exists
        RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("An address is required.");

        // Check if StartDate not equal current date
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)).WithMessage("Start date is required.");

        // Check if endDate exists and its value must be greater or equal StartDate
        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => endDate is null || endDate.Value >= dto.StartDate)
            .WithMessage("End date must be on or after the start date.");

        // An action must have a Reason
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Explain why the row is being corrected.")
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
    }
}