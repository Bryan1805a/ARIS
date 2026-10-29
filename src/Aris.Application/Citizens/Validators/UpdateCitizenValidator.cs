using Aris.Application.Citizens.Dtos;
using FluentValidation;

namespace Aris.Application.Citizens.Validators;

public sealed class UpdateCitizenValidator : AbstractValidator<UpdateCitizenDto>
{
    public UpdateCitizenValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.Gender)
            .Must(g => g is "MALE" or "FEMALE" or "OTHER")
            .WithMessage("Gender must be MALE, FEMALE, or OTHER.");
    }
}