namespace Aris.Application.Transfers.Dtos;

public sealed record TransferItemDecisionDto
{
    public required int ItemId { get; init; }

    public required bool Approve { get; init; }

    /// <summary>Required when <see cref="Approve"/> is false (<c>CK_TRI_Reject</c>).</summary>
    public string? RejectReason { get; init; }
}
