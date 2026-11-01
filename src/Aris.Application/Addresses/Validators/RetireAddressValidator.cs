using Aris.Application.Addresses.Dtos;

using FluentValidation;

namespace Aris.Application.Addresses.Validators;

public sealed class RetireAddressValidator : AbstractValidator<RetireAddressDto> {
    public RetireAddressValidator() {
        RuleFor(x => x.AddressId).GreaterThan(0).WithMessage("An address is required.");

        RuleFor(x => x.ReplacedByAddressId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Select the successor address.")
            .Must((dto, successorId) => successorId != dto.AddressId)
            .WithMessage("An address cannot be replaced by itself.");

        RuleFor(x => x.ValidTo).NotEqual(default(DateOnly)).WithMessage("Valid-to date is required.");
    }
}