namespace Aris.Application.Citizens.Dtos;

/// <summary>One entry in a citizen's residency timeline (<c>CIT-02</c>).</summary>
public sealed record ResidencyPeriodDto
{
    public required int ResidenceId { get; init; }
    public required int AddressId { get; init; }
    public required string AddressText { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    /// <summary><c>ACTIVE</c> or <c>ENDED</c>; <c>VOID</c> rows are excluded.</summary>
    public required string Status { get; init; }
}
