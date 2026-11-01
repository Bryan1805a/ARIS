namespace Aris.Application.Addresses.Dtos;

/// <summary>A citizen currently resident at an address (<c>ADR-02</c>).</summary>
public sealed record AddressResidentDto {
    public required int CitizenId { get; init; }
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required int HouseholdId { get; init; }
    public required string Role { get; init; }
}