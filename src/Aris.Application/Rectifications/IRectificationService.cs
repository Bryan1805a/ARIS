using Aris.Application.Common;
using Aris.Application.Rectifications.Dtos;

namespace Aris.Application.Rectifications;

/// <summary>
/// Void-and-correct workflow for the three append-only tenure tables
/// (<c>HouseholdMembership</c>, <c>Residence</c>, <c>HouseholdAddress</c>) — <c>HIST-01</c>.
/// </summary>
/// <remarks>
/// Each operation marks one erroneous row <c>VOID</c> and inserts a replacement linked by
/// <c>Corrects...Id</c>. Both audit rows share one <c>CorrelationId</c>. The
/// implementation lives in Infrastructure because it needs <see cref="ArisDbContext"/>
/// and a transaction (<c>D18</c>).
/// </remarks>
public interface IRectificationService {
    Task<Result<int>> RectifyResidenceAsync(
        RectifyResidenceDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result<int>> RectifyMembershipAsync(
        RectifyMembershipDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result<int>> RectifyHouseholdAddressAsync(
        RectifyHouseholdAddressDto dto,
        int currentUserId,
        CancellationToken ct = default);
}