using System.Text.Json;

using Aris.Application.Accounts;
using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Developer;
using Aris.Domain.Entities;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Developer;

/// <summary>
/// Privileged local development helpers behind the Developer mode screen.
/// </summary>
/// <remarks>
/// This service deliberately bypasses the authenticated-administrator check used by
/// <see cref="IAccountService"/>. It is a prototype/demo convenience only; the UI
/// labels it as development-only and it must never be exposed in production.
/// Account creation is still transactional and audited.
/// </remarks>
public sealed class DeveloperService : IDeveloperService {
    private const string CreateAccountAction = "CREATE_ACCOUNT";
    private const string SeedDemoAction = "SEED_DEMO_DATA";
    private const int MinimumPasswordLength = 8;

    private readonly ArisDbContext _context;
    private readonly IAuditService _audit;
    private readonly IDemoDataService _demoData;

    public DeveloperService(
        ArisDbContext context,
        IAuditService audit,
        IDemoDataService demoData) {
        _context = context;
        _audit = audit;
        _demoData = demoData;
    }

    public async Task<Result<int>> CreateAccountAsync(
        CreateDeveloperAccountDto dto,
        CancellationToken ct = default) {
        var username = (dto.Username ?? string.Empty).Trim();
        var fullName = (dto.FullName ?? string.Empty).Trim();
        var role = (dto.Role ?? string.Empty).Trim().ToUpperInvariant();
        var password = dto.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(password)) {
            return Result<int>.Failure("Username, full name and password are required.");
        }

        if (username.Length > 50 || fullName.Length > 100) {
            return Result<int>.Failure(
                "Username must be 50 characters or fewer; full name 100 or fewer.");
        }

        if (password.Length < MinimumPasswordLength) {
            return Result<int>.Failure(
                $"Password must contain at least {MinimumPasswordLength} characters.");
        }

        if (role is not ("ADMIN" or "OFFICER")) {
            return Result<int>.Failure("Role must be ADMIN or OFFICER.");
        }

        if (await _context.UserAccounts.AnyAsync(u => u.Username == username, ct)) {
            return Result<int>.Failure($"Username '{username}' is already registered.");
        }

        var correlationId = Guid.NewGuid();
        var account = new UserAccount {
            Username = username,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Status = "ACTIVE",
            FailedLoginCount = 0,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try {
            _context.UserAccounts.Add(account);
            await _context.SaveChangesAsync(ct);

            await _audit.LogAsync(new AuditEntry {
                ActorUserId = account.UserId,
                Action = CreateAccountAction,
                EntityType = "UserAccount",
                EntityId = account.UserId,
                DetailsJson = JsonSerializer.Serialize(new {
                    account.Username,
                    account.Role,
                    account.Status,
                    developerMode = true
                }),
                CorrelationId = correlationId
            }, ct);

            await transaction.CommitAsync(ct);
            return Result<int>.Success(account.UserId);
        } catch (Exception ex) {
            try {
                await transaction.RollbackAsync(ct);
            } catch {
                // A trigger may already have rolled the transaction back.
            }

            await _audit.LogFailureIndependentAsync(new AuditFailureEntry {
                AttemptedUsername = "developer-mode",
                Action = CreateAccountAction,
                EntityType = "UserAccount",
                DetailsJson = JsonSerializer.Serialize(new {
                    username,
                    role,
                    reason = ex.Message,
                    exception = ex.GetType().Name,
                    developerMode = true
                }),
                CorrelationId = correlationId
            }, ct);

            return Result<int>.Failure($"The account could not be created: {ex.Message}");
        }
    }

    public async Task<Result<DemoSeedSummary>> SeedDemoDataAsync(
        CancellationToken ct = default) {
        var admin = await _context.UserAccounts
            .AsNoTracking()
            .Where(u => u.Role == "ADMIN" && u.Status == "ACTIVE")
            .OrderByDescending(u => u.UserId)
            .FirstOrDefaultAsync(ct);

        if (admin is null) {
            return Result<DemoSeedSummary>.Failure(
                "Create an active administrator account first; the demo seed is audited as that administrator.");
        }

        try {
            return await _demoData.SeedAsync(admin.UserId, ct);
        } catch (Exception ex) {
            await _audit.LogFailureIndependentAsync(new AuditFailureEntry {
                ActorUserId = admin.UserId,
                Action = SeedDemoAction,
                EntityType = "DemoData",
                DetailsJson = JsonSerializer.Serialize(new {
                    reason = ex.Message,
                    exception = ex.GetType().Name,
                    developerMode = true
                })
            }, ct);

            return Result<DemoSeedSummary>.Failure(
                $"The demo data could not be seeded: {ex.Message}");
        }
    }
}
