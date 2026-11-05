namespace Aris.Application.TemporaryResidences.Dtos;

public sealed record UpdateTemporaryResidenceDto(
    DateOnly EndDate,
    string StayReason,
    string? HostInfo,
    string? Notes
);

