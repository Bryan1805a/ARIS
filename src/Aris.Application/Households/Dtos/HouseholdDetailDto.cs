namespace Aris.Application.Households.Dtos;

public sealed record HouseholdDetailDto
{
    public required int HouseholdId { get; init; }
    public required string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? InactivatedAt { get; init; }
    public int? CurrentAddressId { get; init; }
    public string? CurrentAddressText { get; init; }
    public int? HeadCitizenId { get; init; }
    public string? HeadFullName { get; init; }
    public IReadOnlyList<HouseholdMemberDto> Members { get; init; }
        = Array.Empty<HouseholdMemberDto>();
    public IReadOnlyList<HouseholdAddressHistoryDto> AddressHistory { get; init; }
        = Array.Empty<HouseholdAddressHistoryDto>();
}
