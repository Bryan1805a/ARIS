namespace Aris.Application.Rectifications.Dtos;

/// <summary>
/// Corrects an erroneous <c>HouseholdAddress</c> row by voiding it and opening a linked
/// replacement (<c>HIST-01</c>).
/// </summary>
/// <remarks>
/// The replacement keeps the source row's household and lifecycle status. When the source
/// is <c>ACTIVE</c>, the address must stay unchanged: moving a household is
/// <c>HOUSEHOLD_MOVE</c> (<c>UC-06</c>), not a rectification.
/// </remarks>
public sealed record RectifyHouseholdAddressDto {
    /// <summary>The row being corrected. It must not already be <c>VOID</c>.</summary>
    public required int HouseholdAddressId { get; init; }

    /// <summary>The corrected address. Must equal the source address for an active row.</summary>
    public required int AddressId { get; init; }

    public required DateOnly StartDate { get; init; }

    /// <summary>Required for an <c>ENDED</c> source; must be null for an <c>ACTIVE</c> source.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>Why the row is wrong. Stored in the audit details.</summary>
    public required string Reason { get; init; }
}