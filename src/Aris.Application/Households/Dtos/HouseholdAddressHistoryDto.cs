namespace Aris.Application.Households.Dtos;

public sealed record HouseholdAddressHistoryDto
{
    public required int HouseholdAddressId { get; init; }
    public required int AddressId { get; init; }
    public required string AddressText { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required string Status { get; init; }
}
