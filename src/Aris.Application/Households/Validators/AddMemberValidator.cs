using Aris.Application.Households.Dtos;
using FluentValidation;

namespace Aris.Application.Households.Validators;

public sealed class AddMemberValidator : AbstractValidator<AddMemberDto>
{
    public AddMemberValidator()
    {
        RuleFor(x => x.HouseholdId)
            .GreaterThan(0).WithMessage("A household is required.");

        RuleFor(x => x.CitizenId)
            .GreaterThan(0).WithMessage("Select a citizen.");

        RuleFor(x => x.EffectiveDate)
            .NotEqual(default(DateOnly)).WithMessage("Effective date is required.");
    }
}
