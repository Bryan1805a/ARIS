using Aris.Application.Accounts;
using Aris.Domain.Entities;

using Xunit;

namespace Aris.UnitTests.Accounts;

public sealed class AccountServiceTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateAccount_WritesSuccessAudit_WithoutPasswordOrHash() {
        // A successful creation must persist the account and record only
        // non-sensitive metadata in the audit entry.
        var (service, repository, audit) = CreateService();
        var admin = Admin();
        repository.Add(admin);

        var result = await service.CreateAccountAsync(
            "officer1", "Officer One", "Password1!", "OFFICER", admin.UserId, Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.True(result.Value is int createdId, "Expected the new account id.");

        var created = Assert.Single(repository.Accounts, a => a.Username == "officer1");

        Assert.Equal(created.UserId, createdId);
        Assert.True(created.MustChangePassword);

        var entry = Assert.Single(audit.SuccessEntries);
        Assert.Equal("CREATE_ACCOUNT", entry.Action);
        Assert.Equal(admin.UserId, entry.ActorUserId);
        Assert.Equal("UserAccount", entry.EntityType);
        Assert.Equal(created.UserId, entry.EntityId);
        Assert.NotNull(entry.DetailsJson);

        var details = entry.DetailsJson!;
        Assert.False(details.Contains("Password1!", StringComparison.Ordinal));
        Assert.False(details.Contains(created.PasswordHash, StringComparison.Ordinal));
        Assert.True(details.Contains("officer1", StringComparison.Ordinal));
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task CreateAccount_DuplicateUsername_ReturnsFailure_AndWritesNoAudit() {
        // Duplicate usernames are expected validation failures, not operations
        // that should produce success or failure audit records.
        var (service, repository, audit) = CreateService();
        var admin = Admin();
        repository.Add(admin);
        repository.Add(Officer("officer1"));

        var result = await service.CreateAccountAsync(
            "officer1", "Officer One", "Password1!", "OFFICER", admin.UserId, Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "already registered",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task CreateAccount_NonAdmin_ReturnsFailure_AndWritesNoAudit() {
        // Authorization is checked before mutation, so a non-admin cannot add
        // an account or create an audit trail entry.
        var (service, repository, audit) = CreateService();
        var officer = Officer("officer1");
        repository.Add(officer);

        var result = await service.CreateAccountAsync(
            "officer2", "Officer Two", "Password1!", "OFFICER",
            officer.UserId, Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "permission",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Single(repository.Accounts);
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task ChangeAccountStatus_WritesLockAndUnlockAudits() {
        // Exercise both directions of the status transition and verify that
        // each operation is attributed to the administrator.
        var (service, repository, audit) = CreateService();
        var admin = Admin();
        var officer = Officer("officer1");
        repository.Add(admin);
        repository.Add(officer);

        var lockResult = await service.ChangeAccountStatusAsync(
            officer.UserId, "LOCKED", admin.UserId, Token);

        Assert.True(lockResult.IsSuccess, string.Join("; ", lockResult.Errors));
        Assert.Equal("LOCKED", officer.Status);

        var unlockResult = await service.ChangeAccountStatusAsync(
            officer.UserId, "ACTIVE", admin.UserId, Token);

        Assert.True(unlockResult.IsSuccess, string.Join("; ", unlockResult.Errors));
        Assert.Equal("ACTIVE", officer.Status);

        Assert.Equal(2, audit.SuccessEntries.Count);
        Assert.Equal("LOCK_ACCOUNT", audit.SuccessEntries[0].Action);
        Assert.Equal("UNLOCK_ACCOUNT", audit.SuccessEntries[1].Action);
        Assert.All(
            audit.SuccessEntries,
            entry => Assert.Equal(admin.UserId, entry.ActorUserId));
        Assert.All(
            audit.SuccessEntries,
            entry => Assert.Equal(officer.UserId, entry.EntityId));
    }

    [Fact]
    public async Task ResetPassword_WritesAudit_WithoutPasswordOrHash() {
        // An administrative reset forces a later self-service change, while
        // audit details must never reveal either plaintext or hashed secrets.
        var (service, repository, audit) = CreateService();
        var admin = Admin();
        var officer = Officer("officer1", "OldPass1!", mustChangePassword: false);
        repository.Add(admin);
        repository.Add(officer);

        var result = await service.ResetPasswordAsync(
            officer.UserId, "NewPass1!", admin.UserId, Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.True(officer.MustChangePassword);
        Assert.Null(officer.PasswordChangedAt);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass1!", officer.PasswordHash));

        var entry = Assert.Single(audit.SuccessEntries);
        Assert.Equal("RESET_PASSWORD", entry.Action);
        Assert.Equal(admin.UserId, entry.ActorUserId);

        var details = entry.DetailsJson!;
        Assert.False(details.Contains("NewPass1!", StringComparison.Ordinal));
        Assert.False(details.Contains(officer.PasswordHash, StringComparison.Ordinal));
        Assert.True(details.Contains("officer1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChangeOwnPassword_WrongCurrentPassword_ReturnsFailure_AndWritesNoAudit() {
        // Rejecting an incorrect current password must leave both the credential
        // and mandatory-change state unchanged.
        var (service, repository, audit) = CreateService();
        var officer = Officer("officer1", "OldPass1!", mustChangePassword: true);
        repository.Add(officer);

        var result = await service.ChangeOwnPasswordAsync(
            officer.UserId, "WrongPass1!", "NewPass1!", Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "incorrect",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.True(BCrypt.Net.BCrypt.Verify("OldPass1!", officer.PasswordHash));
        Assert.True(officer.MustChangePassword);
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task ChangeOwnPassword_Success_WritesAudit_AndClearsMustChange() {
        // A valid self-service change replaces the hash, clears the forced
        // change flag, and records a safe audit event.
        var (service, repository, audit) = CreateService();
        var officer = Officer("officer1", "OldPass1!", mustChangePassword: true);
        repository.Add(officer);

        var result = await service.ChangeOwnPasswordAsync(
            officer.UserId, "OldPass1!", "NewPass1!", Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.False(officer.MustChangePassword);
        Assert.NotNull(officer.PasswordChangedAt);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass1!", officer.PasswordHash));

        var entry = Assert.Single(audit.SuccessEntries);
        Assert.Equal("CHANGE_OWN_PASSWORD", entry.Action);
        Assert.Equal(officer.UserId, entry.ActorUserId);

        var details = entry.DetailsJson!;
        Assert.False(details.Contains("OldPass1!", StringComparison.Ordinal));
        Assert.False(details.Contains("NewPass1!", StringComparison.Ordinal));
        Assert.False(details.Contains(officer.PasswordHash, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateAccount_RepositoryFailure_ReturnsFailure_AndWritesFailureAudit() {
        // Simulate persistence failure to verify the service returns a failure
        // while recording the exception independently from the transaction.
        var (service, repository, audit) = CreateService();
        var admin = Admin();
        repository.Add(admin);
        repository.ThrowOnAdd = new InvalidOperationException("simulated insert failure");

        var result = await service.CreateAccountAsync(
            "officer1", "Officer One", "Password1!", "OFFICER", admin.UserId, Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "simulated insert failure",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(audit.SuccessEntries);

        var failure = Assert.Single(audit.FailureEntries);
        Assert.Equal("CREATE_ACCOUNT", failure.Action);
        Assert.Equal(admin.UserId, failure.ActorUserId);
        Assert.Contains(
            "simulated insert failure",
            failure.DetailsJson!,
            StringComparison.OrdinalIgnoreCase);
    }

    private static (
        AccountService Service,
        FakeUserAccountRepository Repository,
        RecordingAuditService Audit) CreateService() {
        // Keep each test isolated with fresh in-memory repository and audit
        // instances instead of sharing mutable state between scenarios.
        var repository = new FakeUserAccountRepository();
        var audit = new RecordingAuditService();
        var service = new AccountService(repository, audit, new ImmediateUnitOfWork());

        return (service, repository, audit);
    }

    // Build an active administrator for operations that require authorization.
    private static UserAccount Admin() => new() {
        Username = "admin",
        FullName = "Admin",
        PasswordHash = "not-a-real-hash",
        Role = "ADMIN",
        Status = "ACTIVE",
        CreatedAt = DateTime.UtcNow
    };

    // Build a real BCrypt hash so password verification tests exercise the
    // same credential format used by the application service.
    private static UserAccount Officer(
        string username = "officer1",
        string password = "OldPass1!",
        bool mustChangePassword = false) => new() {
            Username = username,
            FullName = "Officer One",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "OFFICER",
            Status = "ACTIVE",
            MustChangePassword = mustChangePassword,
            CreatedAt = DateTime.UtcNow
        };
}