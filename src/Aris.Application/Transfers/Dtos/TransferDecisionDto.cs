namespace Aris.Application.Transfers.Dtos;

/// <summary>Checker input. Every item must have a decision; partial approval is allowed.</summary>
public sealed record TransferDecisionDto
{
    public required int RequestId { get; init; }

    public required IReadOnlyList<TransferItemDecisionDto> Items { get; init; }

    public string? DecisionNote { get; init; }
}
