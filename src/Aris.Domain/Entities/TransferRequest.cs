using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Header of a maker-checker relocation (<c>dbo.TransferRequest</c>, <c>UC-06</c>,
/// <c>D1</c>, <c>D5</c>).
/// </summary>
/// <remarks>
/// Shape is enforced by <c>CK_TR_Shape</c>: each <see cref="RequestType"/> carries
/// exactly the destination fields it needs. <c>CK_TR_Decision</c> requires
/// <c>DecidedBy &lt;&gt; RequestedBy</c> for approved/rejected requests.
/// </remarks>
public sealed class TransferRequest : EntityBase
{
    public int RequestId { get; set; }

    /// <summary><c>JOIN_EXISTING</c>, <c>NEW_HOUSEHOLD</c>, or <c>HOUSEHOLD_MOVE</c>.</summary>
    public string RequestType { get; set; } = null!;

    public int SourceHouseholdId { get; set; }

    /// <summary><c>JOIN_EXISTING</c> only; must differ from <see cref="SourceHouseholdId"/>.</summary>
    public int? DestinationHouseholdId { get; set; }

    /// <summary><c>NEW_HOUSEHOLD</c> and <c>HOUSEHOLD_MOVE</c> only.</summary>
    public int? DestinationAddressId { get; set; }

    /// <summary>Business effective date <c>E</c>.</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary><c>NEW_HOUSEHOLD</c> only.</summary>
    public int? NewHeadCitizenId { get; set; }

    /// <summary>Used when the source head leaves but other members remain.</summary>
    public int? SuccessorHeadCitizenId { get; set; }

    /// <summary>Household created or joined; set on approval.</summary>
    public int? ResultHouseholdId { get; set; }

    /// <summary><c>PENDING</c>, <c>APPROVED</c>, <c>REJECTED</c>, or <c>CANCELLED</c>.</summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>The maker (<c>RequestedBy</c>).</summary>
    public int RequestedBy { get; set; }

    public DateTime RequestedAt { get; set; }

    /// <summary>The checker; must differ from <see cref="RequestedBy"/> when decided.</summary>
    public int? DecidedBy { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? DecisionNote { get; set; }
}
