namespace Aris.Application.CivilStatuses.Dtos;

public sealed record RecordCivilStatusDto(
    string RecordType,
    int CitizenId,
    int? RelatedCitizenId,
    int AddressId,
    DateOnly EventDate,
    DateOnly RegistrationDate,
    string? DocumentNumber,
    string? Notes
);