namespace Aris.Application.TemporaryResidences.Dtos;

public sealed record TemporaryResidenceDto(
    int TempResId,
    int CitizenId,
    string NationalId,
    string FullName,
    int AddressId,
    string AddressText,
    DateOnly StartDate,
    DateOnly EndDate,
    string StayReason,
    string? HostInfo,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    string CreatedByUsername
);

