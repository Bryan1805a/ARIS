using Aris.Application.Households.Dtos;
using FluentValidation;

namespace Aris.Application.Households.Validators;

public sealed class RegisterHouseholdValidator : AbstractValidator<RegisterHouseholdDto>
{
    public RegisterHouseholdValidator()
    {
        RuleFor(x => x.HeadCitizenId)
            .GreaterThan(0).WithMessage("Select the head citizen.");

        RuleFor(x => x.AddressId)
            .GreaterThan(0).WithMessage("Select an active address.");

        RuleFor(x => x.EffectiveDate)
            .NotEqual(default(DateOnly)).WithMessage("Effective date is required.");
    }
}
