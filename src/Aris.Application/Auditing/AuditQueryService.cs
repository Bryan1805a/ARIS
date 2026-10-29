using Aris.Application.Common;
using Aris.Application.Interfaces;

namespace Aris.Application.Auditing;

public class AuditQueryService : IAuditQueryService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IUserSession _userSession;

    public AuditQueryService(
        IAuditRepository auditRepository,
        IUserSession userSession)
    {
        _auditRepository = auditRepository;
        _userSession = userSession;
    }

    public async Task<List<AuditLogDto>> GetRecentAsync(int limit = 100)
    {
        if (!_userSession.IsAuthenticated ||
            _userSession.Role != "ADMIN")
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view audit logs.");
        }

        if (limit < 1 || limit > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Limit must be between 1 and 100.");
        }

        return await _auditRepository.GetRecentAsync(limit);
    }
}