using Aris.Application.Common;
using Aris.Application.Interfaces;

namespace Aris.Application.Auditing;

public class AuditQueryService : IAuditQueryService {
    private readonly IAuditRepository _auditRepository;
    private readonly IUserSession _userSession;

    public AuditQueryService(
        IAuditRepository auditRepository,
        IUserSession userSession) {
        _auditRepository = auditRepository;
        _userSession = userSession;
    }

    public async Task<Result<List<AuditLogDto>>> SearchAsync(
        AuditSearchCriteria criteria,
        CancellationToken ct = default) {
        // Return validation failures as Results so the search UI can display
        // expected input errors without exception handling.
        if (!_userSession.IsAuthenticated
            || !string.Equals(
                _userSession.Role,
                "ADMIN",
                StringComparison.OrdinalIgnoreCase)) {
            return Result<List<AuditLogDto>>.Failure(
                "Only administrators can view audit logs.");
        }

        if (criteria is null) {
            return Result<List<AuditLogDto>>.Failure(
                "Search criteria are required.");
        }

        if (criteria.Limit is < 1 or > 1000) {
            // Search permits a larger page than GetRecentAsync, but still uses
            // an explicit upper bound to protect the query and UI.
            return Result<List<AuditLogDto>>.Failure(
                "Limit must be between 1 and 1000.");
        }

        if (criteria.FromUtc is DateTime from
            && criteria.ToUtcExclusive is DateTime to
            && from >= to) {
            // The upper bound is exclusive, so an empty or reversed interval
            // cannot produce a meaningful time-range search.
            return Result<List<AuditLogDto>>.Failure(
                "From date must be before To date.");
        }

        var result = criteria.Result?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(result)
            && result is not ("SUCCESS" or "FAILURE")) {
            // Normalize the status filter to the two values written by the
            // audit pipeline rather than accepting arbitrary result text.
            return Result<List<AuditLogDto>>.Failure(
                "Result must be SUCCESS or FAILURE.");
        }

        var normalized = criteria with {
            // Trim optional text filters and convert blank values to null so
            // repository queries do not treat whitespace as a real filter.
            Username = Normalize(criteria.Username),
            EntityType = Normalize(criteria.EntityType),
            Action = Normalize(criteria.Action),
            Result = string.IsNullOrWhiteSpace(result) ? null : result
        };

        var auditLogs = await _auditRepository.SearchAsync(normalized, ct);

        return Result<List<AuditLogDto>>.Success(auditLogs);
    }

    private static string? Normalize(string? value) {
        // Null represents an omitted filter consistently across all fields.
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}