using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;

using Xunit;

namespace Aris.Tests;

public sealed class AuditQueryServiceTests {
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Officer_Cannot_Search_AuditLogs() {
        // A signed-in officer is still denied because audit searches expose
        // accountability data reserved for administrators.
        var session = new FakeUserSession();
        session.SignIn(2, "staff02", "OFFICER");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var result = await service.SearchAsync(new AuditSearchCriteria(), Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "administrators",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Search_AuditLogs() {
        // Missing authentication must fail before any repository access occurs.
        var session = new FakeUserSession();
        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var result = await service.SearchAsync(new AuditSearchCriteria(), Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "administrators",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task Admin_Can_Search_AuditLogs() {
        // An administrator should receive repository results and preserve the
        // valid action and limit supplied in the search criteria.
        var session = new FakeUserSession();
        session.SignIn(1, "admin", "ADMIN");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var criteria = new AuditSearchCriteria { Action = "LOGIN", Limit = 100 };
        var result = await service.SearchAsync(criteria, Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.True(repository.WasCalled);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!);
        Assert.Equal("admin", result.Value![0].Username);
        Assert.Equal("LOGIN", result.Value![0].Action);
        Assert.Equal("SUCCESS", result.Value![0].Result);

        Assert.NotNull(repository.LastCriteria);
        Assert.Equal("LOGIN", repository.LastCriteria!.Action);
        Assert.Equal(100, repository.LastCriteria.Limit);
    }

    [Fact]
    public async Task Search_Normalizes_Criteria_Before_Calling_The_Repository() {
        // User-entered filters may contain whitespace and mixed casing; the
        // service normalizes them before handing criteria to the repository.
        var session = new FakeUserSession();
        session.SignIn(1, "admin", "ADMIN");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var criteria = new AuditSearchCriteria {
            Username = "  admin  ",
            EntityType = " UserAccount ",
            Action = " LOGIN ",
            Result = " success "
        };

        var result = await service.SearchAsync(criteria, Token);

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.NotNull(repository.LastCriteria);
        Assert.Equal("admin", repository.LastCriteria!.Username);
        Assert.Equal("UserAccount", repository.LastCriteria.EntityType);
        Assert.Equal("LOGIN", repository.LastCriteria.Action);
        Assert.Equal("SUCCESS", repository.LastCriteria.Result);
    }

    [Fact]
    public async Task Search_Rejects_Invalid_Result() {
        // Result filters are limited to the statuses emitted by the audit
        // pipeline, and invalid input must not reach the repository.
        var session = new FakeUserSession();
        session.SignIn(1, "admin", "ADMIN");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var result = await service.SearchAsync(
            new AuditSearchCriteria { Result = "MAYBE" }, Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "SUCCESS or FAILURE",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task Search_Rejects_From_After_To() {
        // The exclusive upper bound must follow the lower bound to define a
        // meaningful time interval.
        var session = new FakeUserSession();
        session.SignIn(1, "admin", "ADMIN");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var result = await service.SearchAsync(
            new AuditSearchCriteria {
                FromUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ToUtcExclusive = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "From date must be before To date",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task Search_Rejects_Limit_Outside_Range() {
        // Reject out-of-range limits before querying so callers cannot request
        // an unbounded or empty result page.
        var session = new FakeUserSession();
        session.SignIn(1, "admin", "ADMIN");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        var result = await service.SearchAsync(
            new AuditSearchCriteria { Limit = 0 }, Token);

        Assert.True(result.IsFailure);
        Assert.Contains(
            "between 1 and 1000",
            string.Join(" ", result.Errors),
            StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.WasCalled);
    }

    private class FakeUserSession : IUserSession {
        public int? UserId { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }

        public bool IsAuthenticated => UserId.HasValue;

        public void SignIn(int userId, string username, string role) {
            // Model the minimum mutable session state needed by authorization
            // checks without depending on the real authentication subsystem.
            UserId = userId;
            Username = username;
            Role = role;
        }

        public void SignOut() {
            UserId = null;
            Username = null;
            Role = null;
        }
    }

    private class FakeAuditRepository : IAuditRepository {
        public bool WasCalled { get; private set; }
        public AuditSearchCriteria? LastCriteria { get; private set; }

        public Task<List<AuditLogDto>> SearchAsync(
            AuditSearchCriteria criteria,
            CancellationToken ct = default) {
            // Capture the exact criteria passed by the service so tests can
            // verify normalization and validation boundaries.
            WasCalled = true;
            LastCriteria = criteria;
            return Task.FromResult(CreateLogs());
        }

        private static List<AuditLogDto> CreateLogs() {
            // Return one deterministic row; these tests focus on query
            // authorization and criteria handling rather than database paging.
            return new List<AuditLogDto>
            {
                new AuditLogDto
                {
                    AuditLogId = 1,
                    Username = "admin",
                    Action = "LOGIN",
                    EntityType = "UserAccount",
                    EntityId = 1,
                    OccurredAt = DateTime.UtcNow,
                    Result = "SUCCESS"
                }
            };
        }
    }
}