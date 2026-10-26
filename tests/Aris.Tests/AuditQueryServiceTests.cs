using Aris.Application.Auditing;
using Aris.Application.Common;
using Aris.Application.Interfaces;

using Xunit;

namespace Aris.Tests;

public class AuditQueryServiceTests
{
    [Fact]
    public async Task Officer_Cannot_View_AuditLogs()
    {
        var session = new FakeUserSession();
        session.SignIn(2, "staff02", "OFFICER");

        var repository = new FakeAuditRepository();
        var service = new AuditQueryService(repository, session);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetRecentAsync()
        );

        Assert.False(repository.WasCalled);
    }

    [Fact]
    public async Task Admin_Can_View_AuditLogs()
    {
        var session = new FakeUserSession();
        session.SignIn(1, "admin", "ADMIN");

        var repository = new FakeAuditRepository();

        var service = new AuditQueryService(
            repository,
            session
        );

        var result = await service.GetRecentAsync(100);

        Assert.True(repository.WasCalled);
        Assert.Single(result);
        Assert.Equal("admin", result[0].Username);
        Assert.Equal("LOGIN", result[0].Action);
        Assert.Equal("SUCCESS", result[0].Result);
    }

    private class FakeUserSession : IUserSession
    {
        public int? UserId { get; private set; }
        public string? Username { get; private set; }
        public string? Role { get; private set; }

        public bool IsAuthenticated => UserId.HasValue;

        public void SignIn(int userId, string username, string role)
        {
            UserId = userId;
            Username = username;
            Role = role;
        }

        public void SignOut()
        {
            UserId = null;
            Username = null;
            Role = null;
        }
    }

    private class FakeAuditRepository : IAuditRepository
    {
        public bool WasCalled { get; private set; }

        public Task<List<AuditLogDto>> GetRecentAsync(int limit = 100)
        {
            WasCalled = true;

            var auditLogs = new List<AuditLogDto>
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

            return Task.FromResult(auditLogs);
        }
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_View_AuditLogs()
    {
        var session = new FakeUserSession();
        var repository = new FakeAuditRepository();

        var service = new AuditQueryService(
            repository,
            session
        );

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetRecentAsync()
        );

        Assert.False(repository.WasCalled);
    }
}