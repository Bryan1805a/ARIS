namespace Aris.Application.Auditing;

public interface IAuditQueryService
{
    Task<List<AuditLogDto>> GetRecentAsync(int limit = 100);
}