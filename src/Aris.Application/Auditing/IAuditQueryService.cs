using Aris.Application.Common;

namespace Aris.Application.Auditing;

public interface IAuditQueryService {
    Task<Result<List<AuditLogDto>>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken ct = default);
}