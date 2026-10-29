namespace Aris.Application.Households.Dtos;

public sealed record HouseholdSearchCriteria
{
    public int? HouseholdId { get; init; }

    /// <summary>Head name, head National ID, or address text.</summary>
    public string? SearchText { get; init; }

    /// <summary><c>ACTIVE</c> or <c>INACTIVE</c>; null means all.</summary>
    public string? Status { get; init; }
}
