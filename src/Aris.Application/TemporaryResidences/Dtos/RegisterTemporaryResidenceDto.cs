namespace Aris.Application.TemporaryResidences.Dtos;

public sealed record RegisterTemporaryResidenceDto(
    string NationalId,
    int AddressId,
    DateOnly StartDate,
    DateOnly EndDate,
    string StayReason,
    string? HostInfo,
    string? Notes
);

