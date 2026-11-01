namespace Aris.UI.Rectifications;

/// <summary>
/// The source row a rectification starts from (<c>HIST-01</c>).
/// Only the fields that apply to <see cref="Kind"/> are populated.
/// </summary>
public sealed record RectifyTenureRequest {
    public required RectifyTenureKind Kind { get; init; }
    public required int SourceId { get; init; }
    public required string Status { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int? HouseholdId { get; init; }
    public int? AddressId { get; init; }
    public string? Role { get; init; }

    /// <summary>Human-readable owner, for example a citizen name or an address text.</summary>
    public string? OwnerLabel { get; init; }
}