using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Append-only tenure and role of a citizen in a household
/// (<c>dbo.HouseholdMembership</c>, <c>D3</c>).
/// </summary>
/// <remarks>
/// Role changes are close-and-open, never an in-place <c>Role</c> update
/// (trigger <c>51011</c>). At most one active membership per citizen and one active
/// head per household, enforced by filtered unique indexes.
/// </remarks>
public sealed class HouseholdMembership : EntityBase
{
    public int MembershipId { get; set; }

    public int CitizenId { get; set; }

    public int HouseholdId { get; set; }

    /// <summary><c>HEAD</c> or <c>MEMBER</c>.</summary>
    public string Role { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    /// <summary><c>ACTIVE</c>, <c>ENDED</c>, or <c>VOID</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? EndedBy { get; set; }

    public DateTime? EndedAt { get; set; }

    /// <summary>The transfer that opened this row, when applicable (<c>D6</c>).</summary>
    public int? OpenedByRequestId { get; set; }

    /// <summary>The voided row this row corrects (<c>HIST-01</c>).</summary>
    public int? CorrectsMembershipId { get; set; }
}
