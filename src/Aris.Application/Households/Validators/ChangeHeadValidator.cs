using Aris.Application.Households.Dtos;
using FluentValidation;

namespace Aris.Application.Households.Validators;

public sealed class ChangeHeadValidator : AbstractValidator<ChangeHeadDto>
{
    public ChangeHeadValidator()
    {
        RuleFor(x => x.HouseholdId)
            .GreaterThan(0).WithMessage("A household is required.");

        RuleFor(x => x.NewHeadCitizenId)
            .GreaterThan(0).WithMessage("Select the new head.");

        RuleFor(x => x.EffectiveDate)
            .NotEqual(default(DateOnly)).WithMessage("Effective date is required.");
    }
}
