using System.Data;
using System.Text.Json;

using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Accounts;

/// <summary>
/// Creates the first <c>ADMIN</c> on a fresh database (<c>R15</c>, <c>T6</c>).
/// </summary>
/// <remarks>
/// The whole operation runs in one serializable transaction: the "no accounts yet" check
/// and the insert cannot interleave with a second bootstrap attempt. The success audit
/// row is written inside the same transaction; a failure is rolled back and audited on an
/// independent connection (<c>D7</c>).
/// </remarks>
public sealed class BootstrapService : IBootstrapService
{
    private const string Action = "CREATE_FIRST_ADMIN";
    private const int DefaultMinPasswordLength = 8;

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;

    public BootstrapService(ArisDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<bool> HasAnyAccountsAsync(CancellationToken cancellationToken = default)
        => await _context.UserAccounts.AnyAsync(cancellationToken).ConfigureAwait(false);

    public async Task<Result<int>> CreateFirstAdminAsync(
        string username,
        string fullName,
        string password,
        CancellationToken cancellationToken = default)
    {
        username = (username ?? string.Empty).Trim();
        fullName = (fullName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
        {
            return Result<int>.Failure("Username is required and must be at most 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100)
        {
            return Result<int>.Failure("Full name is required and must be at most 100 characters.");
        }

        var minPasswordLength = await GetMinPasswordLengthAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(password) || password.Length < minPasswordLength)
        {
            return Result<int>.Failure(
                $"Password must be at least {minPasswordLength} characters.");
        }

        var correlationId = Guid.NewGuid();

        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            if (await _context.UserAccounts.AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return Result<int>.Failure(
                    "An account already exists. Sign in and create additional accounts from Account Management.");
            }

            var admin = new UserAccount
            {
                Username = username,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "ADMIN",
                Status = "ACTIVE",
                FailedLoginCount = 0,
                MustChangePassword = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserAccounts.Add(admin);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _audit.LogAsync(new AuditEntry
            {
                ActorUserId = admin.UserId,
                Action = Action,
                EntityType = "UserAccount",
                EntityId = admin.UserId,
                DetailsJson = JsonSerializer.Serialize(new { username = admin.Username }),
                CorrelationId = correlationId
            }, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<int>.Success(admin.UserId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await LogFailureAsync(username, correlationId, ex, cancellationToken).ConfigureAwait(false);

            return Result<int>.Failure(
                $"The first administrator could not be created: {ex.Message}");
        }
    }

    private async Task<int> GetMinPasswordLengthAsync(CancellationToken cancellationToken)
    {
        var value = await _context.SystemConfigurations
            .AsNoTracking()
            .Where(c => c.ConfigKey == "PASSWORD_MIN_LENGTH")
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return int.TryParse(value, out var min) && min > 0
            ? min
            : DefaultMinPasswordLength;
    }

    private async Task LogFailureAsync(
        string attemptedUsername,
        Guid correlationId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var connectionString = _context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await _audit.LogFailureIndependentAsync(connectionString, new AuditFailureEntry
        {
            AttemptedUsername = attemptedUsername,
            Action = Action,
            EntityType = "UserAccount",
            DetailsJson = JsonSerializer.Serialize(new
            {
                reason = exception.Message,
                exception = exception.GetType().Name
            }),
            CorrelationId = correlationId
        }, cancellationToken).ConfigureAwait(false);
    }
}
