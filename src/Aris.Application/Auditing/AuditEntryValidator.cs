using System.Text.Json;

namespace Aris.Application.Auditing;

/// <summary>
/// Validates audit entries against the <c>dbo.AuditLog</c> constraints <b>before</b>
/// they reach the database.
/// </summary>
/// <remarks>
/// The database has the final say, but a violation there surfaces as an opaque
/// truncation or CHECK failure at the end of a business transaction. Catching it here
/// turns it into a clear message at the point of the mistake.
///
/// It also enforces one rule the database intentionally does <b>not</b>:
/// <c>CK_Audit_Actor</c> accepts <c>ActorUserId OR AttemptedUsername</c>. For a
/// successful operation the actor must be a real user id, so that requirement is
/// enforced here. That gap is the kind of thing that silently produces an unattributed
/// audit row, which defeats the point of the log.
/// </remarks>
public static class AuditEntryValidator
{
    /// <summary>Validates a successful mutation entry.</summary>
    public static bool TryValidate(AuditEntry? entry, out IReadOnlyList<string> errors)
    {
        var problems = new List<string>();

        if (entry is null)
        {
            errors = new[] { "Audit entry is required." };
            return false;
        }

        if (entry.ActorUserId <= 0)
        {
            problems.Add("ActorUserId must identify the authenticated user. A successful operation cannot be unattributed.");
        }

        ValidateCommon(entry.Action, entry.EntityType, entry.DetailsJson, problems);

        errors = problems;
        return problems.Count == 0;
    }

    /// <summary>Validates a failure entry, which may legitimately have no known actor.</summary>
    public static bool TryValidate(AuditFailureEntry? entry, out IReadOnlyList<string> errors)
    {
        var problems = new List<string>();

        if (entry is null)
        {
            errors = new[] { "Audit failure entry is required." };
            return false;
        }

        var hasActor = entry.ActorUserId is > 0;
        var hasUsername = !string.IsNullOrWhiteSpace(entry.AttemptedUsername);

        if (!hasActor && !hasUsername)
        {
            problems.Add("CK_Audit_Actor requires ActorUserId or AttemptedUsername. A failure with neither cannot be attributed.");
        }

        if (entry.AttemptedUsername is { Length: > AuditFailureEntry.MaxUsernameLength })
        {
            problems.Add($"AttemptedUsername must be at most {AuditFailureEntry.MaxUsernameLength} characters; got {entry.AttemptedUsername.Length}.");
        }

        ValidateCommon(entry.Action, entry.EntityType, entry.DetailsJson, problems);

        errors = problems;
        return problems.Count == 0;
    }

    /// <summary>
    /// Shared column checks. <paramref name="detailsJson"/> must be well-formed JSON
    /// because <c>CK_Audit_Details</c> runs <c>ISJSON(Details) = 1</c>.
    /// </summary>
    private static void ValidateCommon(string action, string entityType, string? detailsJson, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            problems.Add("Action is required.");
        }
        else if (action.Length > AuditEntry.MaxActionLength)
        {
            problems.Add($"Action must be at most {AuditEntry.MaxActionLength} characters; got {action.Length}.");
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            problems.Add("EntityType is required.");
        }
        else if (entityType.Length > AuditEntry.MaxEntityTypeLength)
        {
            problems.Add($"EntityType must be at most {AuditEntry.MaxEntityTypeLength} characters; got {entityType.Length}.");
        }

        if (detailsJson is not null && !IsWellFormedJson(detailsJson))
        {
            problems.Add("DetailsJson must be well-formed JSON, or null. The database rejects anything else (CK_Audit_Details).");
        }
    }

    private static bool IsWellFormedJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
