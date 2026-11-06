namespace Aris.Application.CivilStatuses.Dtos;

public sealed record CivilStatusRecordDto(
    int CivilStatusId,
    string RecordType,
    int CitizenId,
    string CitizenNationalId,
    string CitizenFullName,
    int? RelatedCitizenId,
    string? RelatedNationalId,
    string? RelatedFullName,
    int? AddressId,
    string? AddressText,
    DateOnly EventDate,
    DateOnly RegistrationDate,
    string? DocumentNumber,
    string Status,
    string? Notes,
    string? CancelReason,
    DateTime CreatedAt,
    string CreatedByUsername,
    DateTime? CancelledAt,
    string? CancelledByUsername
);