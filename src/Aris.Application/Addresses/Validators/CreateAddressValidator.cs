using Aris.Application.Addresses.Dtos;
using FluentValidation;

namespace Aris.Application.Addresses.Validators;

public sealed class CreateAddressValidator : AbstractValidator<CreateAddressDto>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.AddressCode)
            .NotEmpty().WithMessage("Address code is required.")
            .MaximumLength(30).WithMessage("Address code must be at most 30 characters.");

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Address line is required.")
            .MaximumLength(200).WithMessage("Address line must be at most 200 characters.");

        RuleFor(x => x.Ward)
            .NotEmpty().WithMessage("Ward is required.")
            .MaximumLength(100).WithMessage("Ward must be at most 100 characters.");

        RuleFor(x => x.District)
            .MaximumLength(100).WithMessage("District must be at most 100 characters.");

        RuleFor(x => x.Province)
            .NotEmpty().WithMessage("Province is required.")
            .MaximumLength(100).WithMessage("Province must be at most 100 characters.");

        RuleFor(x => x.ValidFrom)
            .NotEqual(default(DateOnly)).WithMessage("Valid-from date is required.");
    }
}
