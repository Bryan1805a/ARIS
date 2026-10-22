using System.Data;
using System.Diagnostics;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;
using Aris.Infrastructure.Resilience;
using Microsoft.Data.SqlClient;

namespace Aris.Infrastructure.Auditing;

/// <summary>
/// Writes the append-only audit trail (<c>dbo.AuditLog</c>).
/// </summary>
/// <remarks>
/// <para>
/// The two paths are deliberately different implementations, not two settings of one
/// method (<c>D7</c>, <c>BR-AUD-01</c>/<c>BR-AUD-03</c>):
/// </para>
/// <list type="bullet">
/// <item>
/// <see cref="LogAsync"/> writes through the caller's <see cref="ArisDbContext"/>, so
/// the audit row joins the caller's transaction. The data change and its audit evidence
/// commit or roll back together. A malformed entry or a failed write throws: a mutation
/// must never commit without its audit row.
/// </item>
/// <item>
/// <see cref="LogFailureIndependentAsync"/> opens a <b>new</b> connection via
/// <see cref="SqlResilience"/>, because the business transaction has already rolled back.
/// It is best-effort by contract: it never throws, so a failed audit of a failure can
/// never replace the error the user needs to see.
/// </item>
/// </list>
/// <para>
/// Neither path ever writes a password, password hash, connection string, or token
/// (<c>CON-SEC-01</c>, <c>CON-SEC-02</c>).
/// </para>
/// </remarks>
public sealed class AuditService : IAuditService
{
    /// <summary><c>CK_Audit_Result</c> value for a completed mutation.</summary>
    private const string SuccessResult = "SUCCESS";

    /// <summary><c>CK_Audit_Result</c> value for a rejected or failed attempt.</summary>
    private const string FailureResult = "FAILURE";

    private const string InsertFailureSql = """
        INSERT INTO dbo.AuditLog
            (ActorUserId, AttemptedUsername, Action, EntityType, EntityId,
             OccurredAt, Result, Details, CorrelationId)
        VALUES
            (@ActorUserId, @AttemptedUsername, @Action, @EntityType, @EntityId,
             SYSUTCDATETIME(), @Result, @Details, @CorrelationId);
        """;

    private readonly ArisDbContext _context;

    public AuditService(ArisDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    /// <exception cref="ValidationException">
    /// The entry violates a <c>dbo.AuditLog</c> rule (column width, missing actor,
    /// malformed JSON). Catching it here keeps the failure at the call site instead of
    /// letting SQL Server truncate or reject the row after the business work is done.
    /// </exception>
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (!AuditEntryValidator.TryValidate(entry, out var errors))
        {
            throw new ValidationException(errors);
        }

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = entry.ActorUserId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            OccurredAt = DateTime.UtcNow,
            Result = SuccessResult,
            Details = entry.DetailsJson,
            CorrelationId = entry.CorrelationId
        });

        // No explicit transaction here: the caller owns it. If it rolls back, this row
        // rolls back too - correct, because the operation did not happen.
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Best-effort: once a connection string is supplied, no exception escapes - not a
    /// validation failure, not a connection failure, not a constraint violation. A
    /// failure entry with no actor and no attempted username cannot be attributed
    /// (<c>CK_Audit_Actor</c>), so it is reported through <see cref="Trace"/> and skipped.
    /// The caller is already handling the real error; replacing it with an audit failure
    /// would hide the problem the user must see (<c>R4</c>).
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString"/> is null or blank. That is a composition error,
    /// not a business failure, so it is surfaced during development.
    /// </exception>
    public async Task LogFailureIndependentAsync(
        string connectionString,
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!AuditEntryValidator.TryValidate(entry, out var errors))
        {
            Trace.TraceError(
                "AuditService skipped an invalid independent FAILURE entry: {0}",
                string.Join(" ", errors));
            return;
        }

        try
        {
            // A fresh connection per attempt: anything bound to the rolled-back business
            // transaction is gone. ExecuteAsync wraps open AND insert, so a connection
            // that drops between the two is retried as one unit; it retries only
            // connection-level faults, never constraint or trigger errors. An ambiguous
            // failure (the server committed, the client lost the acknowledgement) may
            // append a duplicate; for an audit trail a duplicate is preferable to silence.
            await SqlResilience.ExecuteAsync(
                async ct =>
                {
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync(ct).ConfigureAwait(false);

                    await using var command = connection.CreateCommand();
                    command.CommandText = InsertFailureSql;
                    command.CommandTimeout = 30;

                    AddParameter(command, "@ActorUserId", SqlDbType.Int, entry.ActorUserId);
                    AddParameter(command, "@AttemptedUsername", SqlDbType.NVarChar, entry.AttemptedUsername, 50);
                    AddParameter(command, "@Action", SqlDbType.VarChar, entry.Action, 50);
                    AddParameter(command, "@EntityType", SqlDbType.VarChar, entry.EntityType, 50);
                    AddParameter(command, "@EntityId", SqlDbType.Int, entry.EntityId);
                    AddParameter(command, "@Result", SqlDbType.VarChar, FailureResult, 10);
                    AddParameter(command, "@Details", SqlDbType.NVarChar, entry.DetailsJson, -1);
                    AddParameter(command, "@CorrelationId", SqlDbType.UniqueIdentifier, entry.CorrelationId);

                    await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    return true;
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Deliberately swallowed after being reported out-of-band. Losing the failure
            // audit is bad; replacing the original error with an audit exception is worse.
            // The connection string is never logged (CON-SEC-02).
            Trace.TraceError(
                "AuditService could not write the independent FAILURE entry for action '{0}' " +
                "(correlation {1}): {2}",
                entry.Action,
                entry.CorrelationId,
                ex.Message);
        }
    }

    /// <summary>
    /// Adds a typed parameter, mapping <c>null</c> to <see cref="DBNull.Value"/> so the
    /// nullable <c>dbo.AuditLog</c> columns behave exactly as the DDL defines them.
    /// </summary>
    private static void AddParameter<T>(
        SqlCommand command,
        string name,
        SqlDbType type,
        T? value,
        int size = 0)
    {
        var parameter = new SqlParameter(name, type)
        {
            Value = value is null ? DBNull.Value : value
        };

        if (size != 0)
        {
            parameter.Size = size;
        }

        command.Parameters.Add(parameter);
    }
}
