using Aris.Application.CivilStatuses.Dtos;

using FluentValidation;

namespace Aris.Application.CivilStatuses.Validators;

public sealed class RecordCivilStatusValidator : AbstractValidator<RecordCivilStatusDto> {
    public RecordCivilStatusValidator() {
        RuleFor(x => x.RecordType)
            .NotEmpty()
            .Must(BeAValidType)
            .WithMessage("Record type must be BIRTH, DEATH, or MARRIAGE.");

        RuleFor(x => x.CitizenId)
            .GreaterThan(0)
            .WithMessage("A citizen must be selected.");

        RuleFor(x => x.RelatedCitizenId)
            .Must((dto, related) =>
                !IsType(dto, "MARRIAGE") || (related.HasValue && related.Value > 0))
            .WithMessage("A spouse must be selected for a marriage record.")
            .Must((dto, related) =>
                IsType(dto, "MARRIAGE") || related is null)
            .WithMessage("Only a marriage record may reference a related citizen.")
            .Must((dto, related) =>
                !related.HasValue || related.Value != dto.CitizenId)
            .WithMessage("A citizen cannot be married to themselves.");

        RuleFor(x => x.AddressId)
            .GreaterThan(0)
            .WithMessage("An address must be selected.");

        RuleFor(x => x.EventDate)
            .NotEmpty()
            .WithMessage("Event date is required.");

        RuleFor(x => x.RegistrationDate)
            .NotEmpty()
            .WithMessage("Registration date is required.")
            .Must((dto, registration) => registration >= dto.EventDate)
            .WithMessage("Registration date cannot be earlier than the event date.");

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(30)
            .WithMessage("Document number cannot exceed 30 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }

    private static bool BeAValidType(string? type)
        => type?.Trim().ToUpperInvariant() is "BIRTH" or "DEATH" or "MARRIAGE";

    private static bool IsType(RecordCivilStatusDto dto, string type)
        => string.Equals(dto.RecordType?.Trim(), type, StringComparison.OrdinalIgnoreCase);
}