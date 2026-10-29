namespace Aris.Application.Transfers.Dtos;

public sealed record TransferItemDto
{
    public required int ItemId { get; init; }
    public required int CitizenId { get; init; }
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required int SourceMembershipId { get; init; }
    public required int SourceResidenceId { get; init; }
    public required string Status { get; init; }
    public string? RejectReason { get; init; }
}
