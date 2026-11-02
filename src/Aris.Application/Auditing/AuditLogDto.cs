namespace Aris.Application.Auditing;

/// <summary>Read model for the audit viewer and detail panel (<c>AUD-01</c>).</summary>
public class AuditLogDto {
    public long AuditLogId { get; set; }

    /// <summary>Actor username, or the attempted username for an unknown-user login.</summary>
    public string Username { get; set; } = "";

    public int? ActorUserId { get; set; }

    public string? AttemptedUsername { get; set; }

    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public int? EntityId { get; set; }

    public DateTime OccurredAt { get; set; }

    public string Result { get; set; } = "";

    public string? Details { get; set; }

    public Guid? CorrelationId { get; set; }
}