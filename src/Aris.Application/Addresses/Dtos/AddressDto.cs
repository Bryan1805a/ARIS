namespace Aris.Application.Addresses.Dtos;

public sealed record AddressDto
{
    public required int AddressId { get; init; }
    public required string AddressCode { get; init; }
    public required string AddressText { get; init; }
    public required string Ward { get; init; }
    public string? District { get; init; }
    public required string Province { get; init; }
    public required string Status { get; init; }
    public required DateOnly ValidFrom { get; init; }
}
