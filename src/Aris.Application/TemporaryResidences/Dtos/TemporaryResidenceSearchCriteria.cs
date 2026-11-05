namespace Aris.Application.TemporaryResidences.Dtos;

public sealed record TemporaryResidenceSearchCriteria(
    string? SearchText = null,
    string? Status = null
);

