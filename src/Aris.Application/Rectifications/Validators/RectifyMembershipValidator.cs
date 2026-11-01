using Aris.Application.Rectifications.Dtos;

using FluentValidation;

namespace Aris.Application.Rectifications.Validators;

public sealed class RectifyMembershipValidator : AbstractValidator<RectifyMembershipDto> {
    public RectifyMembershipValidator() {
        // Check if MembershipId is valid
        RuleFor(x => x.MembershipId).GreaterThan(0).WithMessage("A membership is required.");

        // Check if HouseholdId is valid
        RuleFor(x => x.HouseholdId).GreaterThan(0).WithMessage("A household is required.");

        // Role must not be empty and either HEAD or MEMBER, stop if the first condition fails
        RuleFor(x => x.Role)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("A role is required.")
            .Must(role => role.Trim().ToUpperInvariant() is "HEAD" or "MEMBER")
            .WithMessage("Role must be HEAD or MEMBER.");

        // Start date must not equal to current date
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)).WithMessage("Start date is required.");

        // EndDate must not be empty and has to be greater or equal StartDate
        RuleFor(x => x.EndDate)
            .Must((dto, endDate) => endDate is null || endDate.Value >= dto.StartDate)
            .WithMessage("End date must be on or after the start date.");

        // Every action must have a Reason
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Explain why the row is being corrected.")
            .MaximumLength(500).WithMessage("Reason must be 500 characters or fewer.");
    }
}