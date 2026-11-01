namespace Aris.Application.Addresses.Dtos;

/// <summary>Retire an address and link it to its successor (<c>ADR-03</c>, <c>D9</c>).</summary>
public sealed record RetireAddressDto {
    public required int AddressId { get; init; }
    public required int ReplacedByAddressId { get; init; }
    public required DateOnly ValidTo { get; init; }
}