using Aris.Application.Transfers.Dtos;
using FluentValidation;

namespace Aris.Application.Transfers.Validators;

/// <summary>Mirrors <c>CK_TR_Shape</c> at the Application boundary.</summary>
public sealed class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequestDto>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.RequestType)
            .Must(t => t is "JOIN_EXISTING" or "NEW_HOUSEHOLD" or "HOUSEHOLD_MOVE")
            .WithMessage("Request type must be JOIN_EXISTING, NEW_HOUSEHOLD, or HOUSEHOLD_MOVE.");

        RuleFor(x => x.SourceHouseholdId)
            .GreaterThan(0).WithMessage("Select the source household.");

        RuleFor(x => x.CitizenIds)
            .NotEmpty().WithMessage("Select at least one moving citizen.");

        RuleFor(x => x.EffectiveDate)
            .NotEqual(default(DateOnly)).WithMessage("Effective date is required.");

        RuleFor(x => x.DestinationHouseholdId)
            .NotNull().When(x => x.RequestType == "JOIN_EXISTING")
            .WithMessage("JOIN_EXISTING requires a destination household.");

        RuleFor(x => x.DestinationAddressId)
            .NotNull().When(x => x.RequestType is "NEW_HOUSEHOLD" or "HOUSEHOLD_MOVE")
            .WithMessage("NEW_HOUSEHOLD and HOUSEHOLD_MOVE require a destination address.");

        RuleFor(x => x.NewHeadCitizenId)
            .NotNull().When(x => x.RequestType == "NEW_HOUSEHOLD")
            .WithMessage("NEW_HOUSEHOLD requires the new head citizen.");

        RuleFor(x => x.NewHeadCitizenId)
            .Null().When(x => x.RequestType is "JOIN_EXISTING" or "HOUSEHOLD_MOVE")
            .WithMessage("Only NEW_HOUSEHOLD may specify a new head.");

        RuleFor(x => x.SuccessorHeadCitizenId)
            .Null().When(x => x.RequestType == "HOUSEHOLD_MOVE")
            .WithMessage("HOUSEHOLD_MOVE moves every member; it cannot have a successor head.");

        RuleFor(x => x.DestinationHouseholdId)
            .Null().When(x => x.RequestType is "NEW_HOUSEHOLD" or "HOUSEHOLD_MOVE")
            .WithMessage("Only JOIN_EXISTING may specify a destination household.");

        RuleFor(x => x.DestinationAddressId)
            .Null().When(x => x.RequestType == "JOIN_EXISTING")
            .WithMessage("JOIN_EXISTING cannot specify a destination address.");

        RuleFor(x => x.DestinationHouseholdId)
            .NotEqual(x => x.SourceHouseholdId)
            .When(x => x.RequestType == "JOIN_EXISTING")
            .WithMessage("The destination household must differ from the source.");
    }
}
