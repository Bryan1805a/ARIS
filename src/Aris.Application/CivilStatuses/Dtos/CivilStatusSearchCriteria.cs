namespace Aris.Application.CivilStatuses.Dtos;

public sealed record CivilStatusSearchCriteria(
    string? SearchText = null,
    string? RecordType = null,
    string? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
);