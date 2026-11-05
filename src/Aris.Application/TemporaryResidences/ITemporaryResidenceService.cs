using Aris.Application.Common;
using Aris.Application.TemporaryResidences.Dtos;

namespace Aris.Application.TemporaryResidences;

public interface ITemporaryResidenceService
{
    Task<Result<IEnumerable<TemporaryResidenceDto>>> SearchAsync(
        TemporaryResidenceSearchCriteria criteria,
        CancellationToken ct = default);

    Task<Result<TemporaryResidenceDto?>> GetByIdAsync(
        int tempResId,
        CancellationToken ct = default);

    Task<Result<int>> RegisterAsync(
        RegisterTemporaryResidenceDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> UpdateAsync(
        int tempResId,
        UpdateTemporaryResidenceDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> CancelAsync(
        int tempResId,
        string? cancelReason,
        int currentUserId,
        CancellationToken ct = default);
}

