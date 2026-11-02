using Aris.Application.Auditing;

namespace Aris.Application.Interfaces;

public interface IAuditRepository {
    Task<List<AuditLogDto>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken ct = default);
}