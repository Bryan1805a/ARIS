using Microsoft.Data.SqlClient;

namespace Aris.Infrastructure.Resilience;

/// <summary>
/// Retry helpers for transient SQL Server faults.
/// </summary>
/// <remarks>
/// ARIS runs against a local SQL Server 2022 container, but connections can still
/// fail transiently: Docker Desktop or the container may restart, the host may be
/// under load, or a pooled connection may be dropped. Retrying a small, explicit
/// set of connection, login and lock-availability errors keeps those moments from
/// surfacing as a crash.
///
/// The policy is deliberately narrower than Microsoft.Data.SqlClient's baseline
/// transient-error list. The baseline also contains Azure SQL service-tier numbers
/// (40197, 40501, 40613, 10928/10929, 49918-49920); a local container never returns
/// them, so including them would only delay a real failure.
///
/// Business failures (constraint violations, trigger guard errors, concurrency
/// conflicts) are never retried. See <see cref="IsTransientErrorNumber"/>.
/// </remarks>
public static class SqlResilience {
    /// <summary>Default number of attempts (initial try + retries).</summary>
    public const int DefaultAttempts = 6;

    /// <summary>Default delay before the first retry. Doubles each attempt.</summary>
    public static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Upper bound for a single backoff wait. A container restart or a slow first
    /// start can take a while, so the cap is deliberately generous.
    /// </summary>
    public static readonly TimeSpan DefaultMaxDelay = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Opens a connection, retrying transient faults. Prefer this over
    /// <see cref="SqlConnection.OpenAsync(CancellationToken)"/> so a restarting
    /// database container does not surface to the user as a crash.
    /// </summary>
    public static async Task<SqlConnection> OpenAsync(
        string connectionString,
        int attempts = DefaultAttempts,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

        var connection = new SqlConnection(connectionString);

        try {
            await ExecuteAsync(
                _ => OpenOnceAsync(connection, cancellationToken),
                attempts,
                cancellationToken).ConfigureAwait(false);
            return connection;
        } catch {
            // Do not hand back a half-open connection to the caller on failure.
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Runs <paramref name="operation"/>, retrying while failures are transient and
    /// attempts remain. The final exception is rethrown unchanged, so callers keep
    /// the real <see cref="SqlException"/> (with its number) for diagnostics.
    /// </summary>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int attempts = DefaultAttempts,
        CancellationToken cancellationToken = default,
        TimeSpan? baseDelay = null) {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

        var delay = baseDelay ?? DefaultBaseDelay;

        for (var attempt = 1; ; attempt++) {
            try {
                return await operation(cancellationToken).ConfigureAwait(false);
            } catch (Exception ex) when (attempt < attempts && IsTransient(ex)) {
                await Task.Delay(Backoff(attempt, delay), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// True when the failure is worth retrying. SQL Server reports connection-level
    /// availability problems as specific error numbers; see
    /// <see cref="IsTransientErrorNumber"/>.
    /// </summary>
    public static bool IsTransient(Exception exception) => exception switch {
        SqlException sql => sql.Errors.Cast<SqlError>().Any(e => IsTransientErrorNumber(e.Number)),
        TimeoutException => true,
        _ => exception.InnerException is not null && IsTransient(exception.InnerException)
    };

    /// <summary>
    /// The retry decision for a single SQL error number, split out from
    /// <see cref="IsTransient"/> so the policy can be unit-tested directly: a
    /// <see cref="SqlException"/> cannot be constructed without a real server,
    /// so testing through one would be impossible.
    /// </summary>
    /// <remarks>
    /// Kept to the local SQL Server container's connection, login and lock-availability
    /// failures. Azure SQL service-tier numbers are intentionally absent: they cannot
    /// occur against the local container, and retrying them would delay a real failure.
    /// Anything not listed (constraint violations, trigger guard errors) is a real bug
    /// and must fail fast.
    /// </remarks>
    public static bool IsTransientErrorNumber(int errorNumber) => errorNumber switch {
        233 => true,   // login process not ready while the server/container starts
        1205 => true,  // deadlock victim; the transaction was rolled back
        1222 => true,  // lock request timeout; transient contention
        4060 => true,  // database not open yet during startup/recovery
        10053 => true, // connection aborted
        10054 => true, // connection reset
        10060 => true, // connection timeout
        _ => false
    };

    /// <summary>
    /// Exponential backoff (2s, 4s, 8s ... capped) plus jitter, so several clients
    /// reconnecting after the same container restart do not retry in lockstep.
    /// </summary>
    private static TimeSpan Backoff(int attempt, TimeSpan baseDelay) {
        var exponent = Math.Pow(2, attempt - 1);
        var milliseconds = Math.Min(baseDelay.TotalMilliseconds * exponent, DefaultMaxDelay.TotalMilliseconds);

        var jitter = Random.Shared.NextDouble() * 250;
        return TimeSpan.FromMilliseconds(milliseconds + jitter);
    }

    private static async Task<bool> OpenOnceAsync(SqlConnection connection, CancellationToken cancellationToken) {
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}