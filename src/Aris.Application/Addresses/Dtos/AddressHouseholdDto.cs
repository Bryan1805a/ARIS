namespace Aris.Application.Addresses.Dtos;

/// <summary>A household currently located at an address (<c>ADR-02</c>).</summary>
public sealed record AddressHouseholdDto {
    public required int HouseholdId { get; init; }
    public required string Status { get; init; }
    public string? HeadFullName { get; init; }
    public required int ActiveMemberCount { get; init; }
}