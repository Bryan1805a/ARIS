using Aris.Application.Citizens.Dtos;
using Aris.Application.Common;

namespace Aris.Application.Citizens;

public interface ICitizenService
{
    Task<Result<int>> CreateCitizenAsync(CreateCitizenDto dto, int currentUserId, CancellationToken ct = default);
    Task<Result> UpdateCitizenAsync(int citizenId, UpdateCitizenDto dto, int currentUserId, CancellationToken ct = default);
    Task<Result> DeactivateCitizenAsync(int citizenId, int currentUserId, CancellationToken ct = default);
    Task<Result<CitizenDetailDto?>> GetByIdAsync(int citizenId, CancellationToken ct = default);
    Task<Result<IEnumerable<CitizenDto>>> SearchAsync(CitizenSearchCriteria criteria, CancellationToken ct = default);
}