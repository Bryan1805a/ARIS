namespace Aris.Application.Households.Dtos;

public sealed record AddMemberDto
{
    public required int HouseholdId { get; init; }
    public required int CitizenId { get; init; }
    public required DateOnly EffectiveDate { get; init; }
}
