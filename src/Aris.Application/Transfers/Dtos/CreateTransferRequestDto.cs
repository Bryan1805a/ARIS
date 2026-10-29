namespace Aris.Application.Transfers.Dtos;

/// <summary>Maker input for <c>UC-06</c>. No business state changes until approval.</summary>
public sealed record CreateTransferRequestDto
{
    /// <summary><c>JOIN_EXISTING</c>, <c>NEW_HOUSEHOLD</c>, or <c>HOUSEHOLD_MOVE</c>.</summary>
    public required string RequestType { get; init; }

    public required int SourceHouseholdId { get; init; }

    /// <summary>The citizens moving; each must be active in the source household.</summary>
    public required IReadOnlyList<int> CitizenIds { get; init; }

    /// <summary><c>JOIN_EXISTING</c> only.</summary>
    public int? DestinationHouseholdId { get; init; }

    /// <summary><c>NEW_HOUSEHOLD</c> and <c>HOUSEHOLD_MOVE</c> only.</summary>
    public int? DestinationAddressId { get; init; }

    public required DateOnly EffectiveDate { get; init; }

    /// <summary><c>NEW_HOUSEHOLD</c> only; must be one of the moving citizens.</summary>
    public int? NewHeadCitizenId { get; init; }

    /// <summary>Required when the source head leaves while other members remain.</summary>
    public int? SuccessorHeadCitizenId { get; init; }
}
