namespace Aris.Application.Auditing;

/// <summary>
/// Outcome of an audited operation. Maps to <c>dbo.AuditLog.Result</c>, which the
/// database constrains to exactly these two values (<c>CK_Audit_Result</c>).
/// </summary>
public enum AuditOutcome
{
    Success,
    Failure
}

/// <summary>
/// A business mutation worth recording. Maps to <c>dbo.AuditLog</c>.
/// </summary>
/// <remarks>
/// Column widths come from the schema and are enforced by
/// <see cref="AuditEntryValidator"/> so a violation is caught in the Application
/// layer with a readable message rather than as a truncation error from SQL Server.
/// </remarks>
public sealed record AuditEntry
{
    /// <summary>Max length of <c>Action</c> (<c>varchar(50)</c>).</summary>
    public const int MaxActionLength = 50;

    /// <summary>Max length of <c>EntityType</c> (<c>varchar(50)</c>).</summary>
    public const int MaxEntityTypeLength = 50;

    /// <summary>Who did it. Maps to <c>ActorUserId</c> (FK to <c>UserAccount.UserId</c>).</summary>
    public required int ActorUserId { get; init; }

    /// <summary>What happened, e.g. <c>REGISTER_HOUSEHOLD</c>. Keep it stable - reports group by it.</summary>
    public required string Action { get; init; }

    /// <summary>Entity name, e.g. <c>Household</c>.</summary>
    public required string EntityType { get; init; }

    /// <summary>Primary key of the affected row, when there is one.</summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Optional JSON payload: before/after snapshots plus a reason
    /// (<c>BR-AUD-01</c>). Must be well-formed JSON - <c>CK_Audit_Details</c>
    /// rejects anything else.
    /// </summary>
    public string? DetailsJson { get; init; }

    /// <summary>
    /// Links the several audit rows written by one logical operation
    /// (for example a transfer touching six tables).
    /// </summary>
    public Guid? CorrelationId { get; init; }
}

/// <summary>
/// A failed attempt where the actor may not exist or may not be authenticated yet -
/// chiefly a login with an unknown username (<c>BR-AUD-03</c>).
/// </summary>
/// <remarks>
/// <c>CK_Audit_Actor</c> requires <c>ActorUserId</c> <b>or</b> <c>AttemptedUsername</c>
/// to be present. This type is how a failure is recorded when there is no user id.
/// </remarks>
public sealed record AuditFailureEntry
{
    /// <summary>Max length of <c>AttemptedUsername</c> (<c>nvarchar(50)</c>).</summary>
    public const int MaxUsernameLength = 50;

    /// <summary>Known actor, when there is one. Null for an unknown username.</summary>
    public int? ActorUserId { get; init; }

    /// <summary>The username that was attempted. Required when <see cref="ActorUserId"/> is null.</summary>
    public string? AttemptedUsername { get; init; }

    /// <summary>What was attempted, e.g. <c>LOGIN</c>.</summary>
    public required string Action { get; init; }

    /// <summary>Entity name, e.g. <c>UserAccount</c>.</summary>
    public required string EntityType { get; init; }

    /// <summary>Primary key of the affected row, when there is one.</summary>
    public int? EntityId { get; init; }

    /// <summary>Why it failed, as JSON. Must be well-formed JSON.</summary>
    public string? DetailsJson { get; init; }

    /// <summary>Correlates the rows written by one logical operation.</summary>
    public Guid? CorrelationId { get; init; }
}

/// <summary>
/// Writes the audit trail. Every business mutation must call this
/// (<c>BR-AUD-01</c>); the log is append-only and never deleted (<c>BR-AUD-02</c>).
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="LogAsync"/> for an operation that participates in the caller's
/// transaction: if the business transaction rolls back, the audit row rolls back with
/// it - correct, because the operation did not happen.
/// </para>
/// <para>
/// Use <see cref="LogFailureIndependentAsync"/> after a rollback. It writes on its own
/// connection so the failure survives, which is why it takes a connection string
/// instead of relying on an ambient transaction.
/// </para>
/// </remarks>
public interface IAuditService
{
    /// <summary>
    /// Records a successful mutation inside the caller's transaction. Call this
    /// before committing, so the audit row and the data change commit together.
    /// </summary>
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a FAILURE on an independent connection after the business transaction
    /// has rolled back (<c>BR-AUD-03</c>). Never throws for business reasons: a failed
    /// audit of a failure must not replace the original error the user needs to see.
    /// </summary>
    /// <param name="connectionString">
    /// The same database, but a fresh connection. Anything bound to the rolled-back
    /// transaction would be lost.
    /// </param>
    Task LogFailureIndependentAsync(
        string connectionString,
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default);
}
