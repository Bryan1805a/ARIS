using Microsoft.Data.SqlClient;

namespace Aris.Infrastructure.Resilience;

/// <summary>
/// Retry helpers for transient SQL Server faults.
/// </summary>
/// <remarks>
/// ARIS runs against a local SQL Server 2022 container, but connections can still
/// fail transiently: Docker Desktop or the container may restart, the host may be
/// under load, or a pooled connection may be dropped. Retrying a small, explicit
/// set of connection-level errors keeps those moments from surfacing as a crash.
///
/// Business failures (constraint violations, trigger guard errors, concurrency
/// conflicts) are never retried. See <see cref="IsTransientErrorNumber"/>.
/// </remarks>
public static class SqlResilience
{
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
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

        var connection = new SqlConnection(connectionString);

        try
        {
            await ExecuteAsync(
                _ => OpenOnceAsync(connection, cancellationToken),
                attempts,
                cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
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
        TimeSpan? baseDelay = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempts, 1);

        var delay = baseDelay ?? DefaultBaseDelay;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < attempts && IsTransient(ex))
            {
                await Task.Delay(Backoff(attempt, delay), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// True when the failure is worth retrying. SQL Server reports connection-level
    /// availability problems as specific error numbers; see
    /// <see cref="IsTransientErrorNumber"/>.
    /// </summary>
    public static bool IsTransient(Exception exception) => exception switch
    {
        SqlException sql => sql.Errors.Cast<SqlError>().Any(e => IsTransientErrorNumber(e.Number))
            || sql.Message.Contains("not currently available", StringComparison.OrdinalIgnoreCase),
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
    /// Error numbers that indicate a connection or availability problem rather than a
    /// defect in the statement. Anything not listed (constraint violations, trigger
    /// guard errors) is a real bug and must fail fast rather than be retried.
    /// </remarks>
    public static bool IsTransientErrorNumber(int errorNumber) => errorNumber switch
    {
        4060 => true,   // cannot open database requested by the login
        40197 => true,  // service error processing the request
        40501 => true,  // service busy
        40613 => true,  // database not currently available (container starting/restarting)
        10928 or 10929 => true, // resource limit reached
        10053 or 10054 or 10060 => true, // connection dropped / timed out
        49918 or 49919 or 49920 => true, // too many operations for the subscription
        _ => false
    };

    /// <summary>
    /// Exponential backoff (2s, 4s, 8s ... capped) plus jitter, so several clients
    /// reconnecting after the same container restart do not retry in lockstep.
    /// </summary>
    private static TimeSpan Backoff(int attempt, TimeSpan baseDelay)
    {
        var exponent = Math.Pow(2, attempt - 1);
        var milliseconds = Math.Min(baseDelay.TotalMilliseconds * exponent, DefaultMaxDelay.TotalMilliseconds);

        var jitter = Random.Shared.NextDouble() * 250;
        return TimeSpan.FromMilliseconds(milliseconds + jitter);
    }

    private static async Task<bool> OpenOnceAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
