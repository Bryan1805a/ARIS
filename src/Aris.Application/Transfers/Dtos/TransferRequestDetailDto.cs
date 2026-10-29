namespace Aris.Application.Transfers.Dtos;

public sealed record TransferRequestDetailDto
{
    public required int RequestId { get; init; }
    public required string RequestType { get; init; }
    public required int SourceHouseholdId { get; init; }
    public string? SourceAddressText { get; init; }
    public int? DestinationHouseholdId { get; init; }
    public int? DestinationAddressId { get; init; }
    public string? DestinationAddressText { get; init; }
    public required DateOnly EffectiveDate { get; init; }
    public int? NewHeadCitizenId { get; init; }
    public int? SuccessorHeadCitizenId { get; init; }
    public int? ResultHouseholdId { get; init; }
    public required string Status { get; init; }
    public required int RequestedBy { get; init; }
    public string? RequestedByName { get; init; }
    public required DateTime RequestedAt { get; init; }
    public int? DecidedBy { get; init; }
    public string? DecidedByName { get; init; }
    public DateTime? DecidedAt { get; init; }
    public string? DecisionNote { get; init; }
    public IReadOnlyList<TransferItemDto> Items { get; init; }
        = Array.Empty<TransferItemDto>();
}
