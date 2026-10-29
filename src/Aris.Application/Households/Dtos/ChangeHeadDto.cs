namespace Aris.Application.Households.Dtos;

public sealed record ChangeHeadDto
{
    public required int HouseholdId { get; init; }
    public required int NewHeadCitizenId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
}
