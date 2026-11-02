using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;
using Aris.Domain.Entities;

namespace Aris.UnitTests;

internal sealed class ImmediateUnitOfWork : IUnitOfWork {
    public int ExecutionCount { get; private set; }

    public Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken ct = default) {
        // Execute immediately instead of opening a real transaction; tests can
        // still verify that application mutations use the unit-of-work boundary.
        ExecutionCount++;
        return action(ct);
    }
}

internal sealed class RecordingAuditService : IAuditService {
    // Keep separate collections so tests can distinguish committed success
    // audits from failure audits written outside the business transaction.
    public List<AuditEntry> SuccessEntries { get; } = new();
    public List<AuditFailureEntry> FailureEntries { get; } = new();

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default) {
        // Store the original entry for assertions about actor, action, and
        // correlation metadata without requiring an audit database.
        SuccessEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogFailureIndependentAsync(
        string connectionString,
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default) {
        FailureEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogLoginFailureAsync(
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default) {
        FailureEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task LogFailureIndependentAsync(
        AuditFailureEntry entry,
        CancellationToken cancellationToken = default) {
        FailureEntries.Add(entry);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserAccountRepository : IUserAccountRepository {
    private readonly List<UserAccount> _accounts = new();
    private int _nextId = 1;

    public IReadOnlyList<UserAccount> Accounts => _accounts;
    public Exception? ThrowOnAdd { get; set; }
    public Exception? ThrowOnUpdate { get; set; }

    public void Add(UserAccount account) {
        // Assign deterministic IDs so service tests can refer to created
        // accounts without depending on database identity generation.
        if (account.UserId == 0) {
            account.UserId = _nextId++;
        } else {
            _nextId = Math.Max(_nextId, account.UserId + 1);
        }

        _accounts.Add(account);
    }

    public Task<UserAccount?> GetByUsernameAsync(string username) {
        return Task.FromResult(
            _accounts.FirstOrDefault(a => a.Username == username));
    }

    public Task<List<UserAccount>> GetAllAsync() {
        return Task.FromResult(_accounts.ToList());
    }

    public Task AddAsync(UserAccount account) {
        // Tests can inject persistence failures to exercise failure auditing
        // and Result-based error handling.
        if (ThrowOnAdd is not null) {
            throw ThrowOnAdd;
        }

        Add(account);
        return Task.CompletedTask;
    }

    public Task<UserAccount?> GetByIdAsync(int userId) {
        return Task.FromResult(
            _accounts.FirstOrDefault(a => a.UserId == userId));
    }

    public Task UpdateAsync(UserAccount account) {
        if (ThrowOnUpdate is not null) {
            throw ThrowOnUpdate;
        }

        var index = _accounts.FindIndex(a => a.UserId == account.UserId);
        // Replace the stored reference to model an update while also allowing
        // tests to exercise behavior when an entity is not already present.
        if (index >= 0) {
            _accounts[index] = account;
        } else {
            _accounts.Add(account);
        }

        return Task.CompletedTask;
    }
}

internal sealed class FakeSystemConfigurationRepository
    : ISystemConfigurationRepository {
    private readonly Dictionary<string, SystemConfiguration> _configurations =
        new(StringComparer.OrdinalIgnoreCase);

    public Exception? ThrowOnUpdate { get; set; }

    public void Add(string key, string value, string? description = null) {
        // Ordinal-ignore-case lookup matches configuration keys regardless of
        // the casing used by a caller.
        _configurations[key] = new SystemConfiguration {
            ConfigKey = key,
            ConfigValue = value,
            Description = description,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public Task<string?> GetValueAsync(string configKey) {
        return Task.FromResult(
            _configurations.TryGetValue(configKey, out var configuration)
                ? configuration.ConfigValue
                : null);
    }

    public Task<List<SystemConfiguration>> GetAllAsync() {
        return Task.FromResult(
            _configurations.Values
                .OrderBy(c => c.ConfigKey)
                .ToList());
    }

    public Task<bool> UpdateAsync(
        string configKey,
        string configValue,
        int updatedBy) {
        if (ThrowOnUpdate is not null) {
            throw ThrowOnUpdate;
        }

        if (!_configurations.TryGetValue(configKey, out var configuration)) {
            // Return false for a missing key so the service can report a
            // domain-level failure without throwing.
            return Task.FromResult(false);
        }

        configuration.ConfigValue = configValue;
        configuration.UpdatedAt = DateTime.UtcNow;
        configuration.UpdatedBy = updatedBy;

        return Task.FromResult(true);
    }
}

internal sealed class FakeUserSession : IUserSession {
    public FakeUserSession(
        int? userId = null,
        string? username = null,
        string? role = null) {
        UserId = userId;
        Username = username;
        Role = role;
    }

    public int? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SignIn(int userId, string username, string role) {
        // Keep session state mutable so tests can model login-dependent
        // authorization and switch actors between operations.
        UserId = userId;
        Username = username;
        Role = role;
    }

    public void SignOut() {
        // Clearing every identity field prevents stale role or username data
        // from making a signed-out session appear valid.
        UserId = null;
        Username = null;
        Role = null;
    }
}