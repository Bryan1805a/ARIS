using Aris.Application.Common;
using Aris.Application.Households.Dtos;

namespace Aris.Application.Households;

/// <summary>
/// Household lifecycle and membership use cases (<c>HH-01</c>, <c>HH-02</c>,
/// <c>HH-04</c>, <c>HH-06</c>, <c>SRCH-02</c>).
/// </summary>
public interface IHouseholdService
{
    Task<Result<int>> RegisterHouseholdAsync(
        RegisterHouseholdDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> AddMemberAsync(
        AddMemberDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> ChangeHeadAsync(
        ChangeHeadDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result<HouseholdDetailDto?>> GetByIdAsync(
        int householdId,
        CancellationToken ct = default);

    Task<Result<IEnumerable<HouseholdDto>>> SearchAsync(
        HouseholdSearchCriteria criteria,
        CancellationToken ct = default);
}
