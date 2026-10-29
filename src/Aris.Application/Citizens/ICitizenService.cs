using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;

namespace Aris.Application.Citizens;

public interface ICitizenService
{
    Task<Result<int>> CreateCitizenAsync(CreateCitizenDto dto, int currentUserId, CancellationToken ct = default);
    Task<Result> UpdateCitizenAsync(int citizenId, UpdateCitizenDto dto, int currentUserId, CancellationToken ct = default);
    /// <summary>
    /// Deactivates a citizen (<c>CIT-04</c>). When the citizen is the household head and
    /// other members remain, <paramref name="successorHeadCitizenId"/> is required.
    /// </summary>
    Task<Result> DeactivateCitizenAsync(
        int citizenId,
        int currentUserId,
        int? successorHeadCitizenId = null,
        DateOnly? effectiveDate = null,
        CancellationToken ct = default);
    Task<Result<CitizenDetailDto?>> GetByIdAsync(int citizenId, CancellationToken ct = default);
    Task<Result<IEnumerable<CitizenDto>>> SearchAsync(CitizenSearchCriteria criteria, CancellationToken ct = default);
}