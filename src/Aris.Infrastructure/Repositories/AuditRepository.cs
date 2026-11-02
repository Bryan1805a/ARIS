using Aris.Application.Auditing;
using Aris.Application.Interfaces;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository {
    private readonly ArisDbContext _context;

    public AuditRepository(ArisDbContext context) {
        _context = context;
    }

    public async Task<List<AuditLogDto>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(criteria);

        var query =
            from audit in _context.AuditLogs.AsNoTracking()
            join user in _context.UserAccounts.AsNoTracking()
                on audit.ActorUserId equals (int?)user.UserId
                into userGroup
            from user in userGroup.DefaultIfEmpty()
            select new { audit, user };

        if (criteria.FromUtc is DateTime from) {
            query = query.Where(x => x.audit.OccurredAt >= from);
        }

        if (criteria.ToUtcExclusive is DateTime to) {
            query = query.Where(x => x.audit.OccurredAt < to);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Username)) {
            var term = criteria.Username.Trim();
            query = query.Where(x =>
                (x.user != null && x.user.Username.Contains(term))
                || (x.audit.AttemptedUsername != null
                    && x.audit.AttemptedUsername.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(criteria.EntityType)) {
            var entityType = criteria.EntityType.Trim();
            query = query.Where(x => x.audit.EntityType.Contains(entityType));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action)) {
            var action = criteria.Action.Trim();
            query = query.Where(x => x.audit.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Result)) {
            var result = criteria.Result.Trim().ToUpperInvariant();
            query = query.Where(x => x.audit.Result == result);
        }

        return await query
            .OrderByDescending(x => x.audit.OccurredAt)
            .ThenByDescending(x => x.audit.AuditLogId)
            .Take(criteria.Limit)
            .Select(x => new AuditLogDto {
                AuditLogId = x.audit.AuditLogId,
                Username = x.user != null
                    ? x.user.Username
                    : (x.audit.AttemptedUsername ?? ""),
                ActorUserId = x.audit.ActorUserId,
                AttemptedUsername = x.audit.AttemptedUsername,
                Action = x.audit.Action,
                EntityType = x.audit.EntityType,
                EntityId = x.audit.EntityId,
                OccurredAt = x.audit.OccurredAt,
                Result = x.audit.Result,
                Details = x.audit.Details,
                CorrelationId = x.audit.CorrelationId
            })
            .ToListAsync(ct);
    }
}