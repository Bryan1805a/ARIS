using Aris.Application.Auditing;

namespace Aris.Application.Interfaces;

public interface IAuditRepository
{
    Task<List<AuditLogDto>> GetRecentAsync(int limit = 100);
}