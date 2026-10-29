namespace Aris.Application.Households.Dtos;

public sealed record HouseholdMemberDto
{
    public required int MembershipId { get; init; }
    public required int CitizenId { get; init; }
    public required string NationalId { get; init; }
    public required string FullName { get; init; }
    public required string Role { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required string Status { get; init; }
}
