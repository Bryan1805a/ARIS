namespace Aris.Application.Households.Dtos;

public sealed record HouseholdDto
{
    public required int HouseholdId { get; init; }
    public required string Status { get; init; }
    public int? HeadCitizenId { get; init; }
    public string? HeadFullName { get; init; }
    public string? CurrentAddressText { get; init; }
    public required int ActiveMemberCount { get; init; }
}
