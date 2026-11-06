using Aris.Application.CivilStatuses.Dtos;
using Aris.Application.Common;

namespace Aris.Application.CivilStatuses;

public interface ICivilStatusService {
    Task<Result<IEnumerable<CivilStatusRecordDto>>> SearchAsync(
        CivilStatusSearchCriteria criteria,
        CancellationToken ct = default);

    Task<Result<CivilStatusRecordDto?>> GetByIdAsync(
        int civilStatusId,
        CancellationToken ct = default);

    Task<Result<int>> RegisterAsync(
        RecordCivilStatusDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> CancelAsync(
        int civilStatusId,
        CancelCivilStatusDto dto,
        int currentUserId,
        CancellationToken ct = default);
}