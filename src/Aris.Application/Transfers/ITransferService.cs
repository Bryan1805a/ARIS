using Aris.Application.Common;
using Aris.Application.Transfers.Dtos;

namespace Aris.Application.Transfers;

/// <summary>
/// Maker-checker relocation workflow (<c>UC-06</c>, <c>D1</c>, <c>D5</c>).
/// </summary>
public interface ITransferService
{
    Task<Result<int>> CreateRequestAsync(
        CreateTransferRequestDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> DecideAsync(
        TransferDecisionDto dto,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> CancelAsync(
        int requestId,
        int currentUserId,
        string? note = null,
        CancellationToken ct = default);

    Task<Result<IEnumerable<TransferRequestSummaryDto>>> GetRequestsAsync(
        TransferSearchCriteria criteria,
        CancellationToken ct = default);

    Task<Result<TransferRequestDetailDto?>> GetByIdAsync(
        int requestId,
        CancellationToken ct = default);
}
