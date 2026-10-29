namespace Aris.Application.Households.Dtos;

public sealed record RegisterHouseholdDto
{
    public required int HeadCitizenId { get; init; }
    public required int AddressId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
}
