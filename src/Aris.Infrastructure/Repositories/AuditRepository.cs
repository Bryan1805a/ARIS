using Aris.Application.Auditing;
using Aris.Application.Interfaces;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly ArisDbContext _context;

    public AuditRepository(ArisDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLogDto>> GetRecentAsync(int limit = 100)
    {
        return await (
            from audit in _context.AuditLogs.AsNoTracking()

            join user in _context.UserAccounts.AsNoTracking()
                on audit.ActorUserId equals (int?)user.UserId
                into userGroup

            from user in userGroup.DefaultIfEmpty()

            orderby audit.OccurredAt descending, audit.AuditLogId descending

            select new AuditLogDto
            {
                AuditLogId = audit.AuditLogId,

                Username = user != null
                    ? user.Username
                    : (audit.AttemptedUsername ?? ""),

                Action = audit.Action,
                EntityType = audit.EntityType,
                EntityId = audit.EntityId,
                OccurredAt = audit.OccurredAt,
                Result = audit.Result,
                Details = audit.Details
            }
        )
        .Take(limit)
        .ToListAsync();
    }
}