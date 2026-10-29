namespace Aris.Application.Transfers.Dtos;

public sealed record TransferSearchCriteria
{
    /// <summary><c>PENDING</c>, <c>APPROVED</c>, <c>REJECTED</c>, or <c>CANCELLED</c>; null means all.</summary>
    public string? Status { get; init; }

    /// <summary>Optional maker filter for "my requests".</summary>
    public int? RequestedBy { get; init; }
}
