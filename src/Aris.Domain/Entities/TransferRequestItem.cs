using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// One moving citizen per transfer request; supports partial approval
/// (<c>dbo.TransferRequestItem</c>, <c>D5</c>).
/// </summary>
/// <remarks>
/// Composite foreign keys guarantee the source membership and residence belong to
/// the same citizen as this item. <c>UX_TRI_OneOpenPerCitizen</c> allows at most one
/// <c>PENDING</c> item per citizen across all requests.
/// </remarks>
public sealed class TransferRequestItem : EntityBase
{
    public int ItemId { get; set; }

    public int RequestId { get; set; }

    public int CitizenId { get; set; }

    public int SourceMembershipId { get; set; }

    public int SourceResidenceId { get; set; }

    /// <summary><c>PENDING</c>, <c>APPROVED</c>, <c>REJECTED</c>, or <c>CANCELLED</c>.</summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>Required when <see cref="Status"/> is <c>REJECTED</c> (<c>CK_TRI_Reject</c>).</summary>
    public string? RejectReason { get; set; }
}
