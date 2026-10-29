namespace Aris.Application.Transfers.Dtos;

public sealed record TransferRequestSummaryDto
{
    public required int RequestId { get; init; }
    public required string RequestType { get; init; }
    public required string Status { get; init; }
    public required int SourceHouseholdId { get; init; }
    public string? SourceAddressText { get; init; }
    public int? DestinationHouseholdId { get; init; }
    public int? DestinationAddressId { get; init; }
    public string? DestinationAddressText { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public required int RequestedBy { get; init; }
    public string? RequestedByName { get; init; }
    public required DateTime RequestedAt { get; init; }
    public required int ItemCount { get; init; }
    public required int ApprovedItemCount { get; init; }
    public required int RejectedItemCount { get; init; }
}
