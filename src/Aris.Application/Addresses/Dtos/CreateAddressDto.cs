namespace Aris.Application.Addresses.Dtos;

public sealed record CreateAddressDto
{
    public required string AddressCode { get; init; }
    public required string AddressLine { get; init; }
    public required string Ward { get; init; }
    public string? District { get; init; }
    public required string Province { get; init; }
    public required DateOnly ValidFrom { get; init; }
}
