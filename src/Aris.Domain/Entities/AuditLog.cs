namespace Aris.Domain.Entities;

/// <summary>
/// Immutable audit record (<c>dbo.AuditLog</c>, <c>D7</c>).
/// </summary>
/// <remarks>
/// Append-only: never update or delete a row (trigger <c>51030</c>, and the
/// application role is denied both operations). This table has no <c>rowversion</c>
/// and therefore does not derive from <see cref="Common.EntityBase"/>.
/// </remarks>
public sealed class AuditLog
{
    public long AuditLogId { get; set; }

    /// <summary>Required unless <see cref="AttemptedUsername"/> is set (<c>CK_Audit_Actor</c>).</summary>
    public int? ActorUserId { get; set; }

    /// <summary>Used for a failed login with an unknown username.</summary>
    public string? AttemptedUsername { get; set; }

    /// <summary>Stable uppercase action name, at most 50 characters.</summary>
    public string Action { get; set; } = null!;

    /// <summary>Singular table name, at most 50 characters.</summary>
    public string EntityType { get; set; } = null!;

    public int? EntityId { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary><c>SUCCESS</c> or <c>FAILURE</c>.</summary>
    public string Result { get; set; } = null!;

    /// <summary>JSON validated by <c>CK_Audit_Details</c> (<c>ISJSON</c>).</summary>
    public string? Details { get; set; }

    /// <summary>Groups every audit row produced by one business action.</summary>
    public Guid? CorrelationId { get; set; }
}
