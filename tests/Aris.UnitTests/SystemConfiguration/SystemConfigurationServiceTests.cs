using Aris.Application.SystemConfiguration;

using Xunit;

namespace Aris.UnitTests.SystemConfigurationTests;

public sealed class SystemConfigurationServiceTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Update_WritesAudit_WithBeforeAndAfter() {
        // A successful update must persist the new value and capture both
        // sides of the change in its audit metadata.
        var (service, repository, audit) = CreateService();

        var result = await service.UpdateAsync("MAX_FAILED_LOGINS", "3", Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal("3", await repository.GetValueAsync("MAX_FAILED_LOGINS"));

        var entry = Assert.Single(audit.SuccessEntries);
        Assert.Equal("UPDATE_SYSTEM_CONFIGURATION", entry.Action);
        Assert.Equal(1, entry.ActorUserId);
        Assert.Equal("SystemConfiguration", entry.EntityType);
        Assert.Null(entry.EntityId);

        var details = entry.DetailsJson!;
        Assert.True(details.Contains("MAX_FAILED_LOGINS", StringComparison.Ordinal));
        Assert.True(details.Contains("\"before\":\"5\"", StringComparison.Ordinal));
        Assert.True(details.Contains("\"after\":\"3\"", StringComparison.Ordinal));
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task Update_InvalidValue_ReturnsFailure_AndWritesNoAudit() {
        // Configuration values are constrained to positive integers, so invalid
        // input must leave both storage and audit state unchanged.
        var (service, repository, audit) = CreateService();

        var result = await service.UpdateAsync("MAX_FAILED_LOGINS", "abc", Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "positive integer",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal("5", await repository.GetValueAsync("MAX_FAILED_LOGINS"));
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task Update_NonAdmin_ReturnsFailure_AndWritesNoAudit() {
        // Authorization is evaluated from the live session before any update
        // or audit record is attempted.
        var (service, repository, audit) = CreateService("OFFICER", 2);

        var result = await service.UpdateAsync("MAX_FAILED_LOGINS", "3", Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "administrators",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal("5", await repository.GetValueAsync("MAX_FAILED_LOGINS"));
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task Update_MissingKey_ReturnsFailure_AndWritesNoAudit() {
        // Updating an unknown key is an expected domain failure, not an
        // exceptional operation that should create an audit entry.
        var (service, _, audit) = CreateService();

        var result = await service.UpdateAsync("UNKNOWN_KEY", "1", Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "not found",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(audit.SuccessEntries);
        Assert.Empty(audit.FailureEntries);
    }

    [Fact]
    public async Task Update_RepositoryFailure_ReturnsFailure_AndWritesFailureAudit() {
        // Persistence errors should produce a failure Result and an independent
        // failure audit, while never recording a success audit.
        var (service, repository, audit) = CreateService();
        repository.ThrowOnUpdate = new InvalidOperationException("simulated update failure");

        var result = await service.UpdateAsync("MAX_FAILED_LOGINS", "3", Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "simulated update failure",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(audit.SuccessEntries);

        var failure = Assert.Single(audit.FailureEntries);
        Assert.Equal("UPDATE_SYSTEM_CONFIGURATION", failure.Action);
        Assert.Equal(1, failure.ActorUserId);
        Assert.Contains(
            "MAX_FAILED_LOGINS",
            failure.DetailsJson!,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAll_ReturnsDtos_ForAnAdmin() {
        // Verify that an administrator can read the configured values through
        // DTOs rather than receiving repository entities directly.
        var (service, _, _) = CreateService();

        var result = await service.GetAllAsync(Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(
            result.Value,
            dto => dto.ConfigKey == "MAX_FAILED_LOGINS" && dto.ConfigValue == "5");
    }

    private static (
        SystemConfigurationService Service,
        FakeSystemConfigurationRepository Repository,
        RecordingAuditService Audit) CreateService(
            string role = "ADMIN",
            int userId = 1) {
        // Seed the same baseline configuration for each test to keep assertions
        // deterministic and avoid sharing mutable repository state.
        var repository = new FakeSystemConfigurationRepository();
        repository.Add("MAX_FAILED_LOGINS", "5");
        repository.Add("LOCKOUT_MINUTES", "15");

        var audit = new RecordingAuditService();
        var session = new FakeUserSession(userId, "it-admin", role);
        var service = new SystemConfigurationService(
            repository, session, audit, new ImmediateUnitOfWork());

        return (service, repository, audit);
    }
}