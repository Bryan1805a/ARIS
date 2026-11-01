namespace Aris.Application.Rectifications.Dtos;

/// <summary>
/// Corrects an erroneous <c>HouseholdMembership</c> row by voiding it and opening a linked
/// replacement (<c>HIST-01</c>).
/// </summary>
/// <remarks>
/// The replacement keeps the source row's citizen and lifecycle status. When the source
/// is <c>ACTIVE</c>, household and role must stay unchanged: a current-state change is a
/// transfer (<c>UC-06</c>) or a head change (<c>HH-04</c>), not a rectification.
/// </remarks>
public sealed record RectifyMembershipDto {
    /// <summary>The row being corrected. It must not already be <c>VOID</c>.</summary>
    public required int MembershipId { get; init; }

    /// <summary>The corrected household. Must equal the source household for an active row.</summary>
    public required int HouseholdId { get; init; }

    /// <summary><c>HEAD</c> or <c>MEMBER</c>. Must equal the source role for an active row.</summary>
    public required string Role { get; init; }

    public required DateOnly StartDate { get; init; }

    /// <summary>Required for an <c>ENDED</c> source; must be null for an <c>ACTIVE</c> source.</summary>
    public DateOnly? EndDate { get; init; }

    /// <summary>Why the row is wrong. Stored in the audit details.</summary>
    public required string Reason { get; init; }
}