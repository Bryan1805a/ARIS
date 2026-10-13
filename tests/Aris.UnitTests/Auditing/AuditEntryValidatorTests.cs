using Aris.Application.Auditing;
using Xunit;

namespace Aris.UnitTests.Auditing;

/// <summary>
/// Every rule here corresponds to a real database constraint on <c>dbo.AuditLog</c>.
/// Catching a violation in the Application layer gives a readable message; letting it
/// reach SQL Server gives a truncation or CHECK failure at the end of a business
/// transaction, which is far harder to diagnose.
/// </summary>
public sealed class AuditEntryValidatorTests
{
    private static AuditEntry ValidEntry() => new()
    {
        ActorUserId = 1,
        Action = "REGISTER_HOUSEHOLD",
        EntityType = "Household",
        EntityId = 42
    };

    [Fact]
    public void ValidEntry_Passes()
    {
        Assert.True(AuditEntryValidator.TryValidate(ValidEntry(), out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidEntry_WithJsonDetails_Passes()
    {
        var entry = ValidEntry() with { DetailsJson = """{"before":null,"after":{"status":"ACTIVE"}}""" };

        Assert.True(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Empty(errors);
    }

    // ---- ActorUserId: the rule the database does NOT enforce -------------------
    // CK_Audit_Actor accepts "ActorUserId OR AttemptedUsername", so a SUCCESS row
    // with neither would be accepted by SQL Server and read as unattributed.

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Success_WithoutRealActor_Fails(int actorUserId)
    {
        var entry = ValidEntry() with { ActorUserId = actorUserId };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains("unattributed", StringComparison.OrdinalIgnoreCase));
    }

    // ---- Action: varchar(50) ---------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Action_Blank_Fails(string action)
    {
        var entry = ValidEntry() with { Action = action };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains("Action is required", StringComparison.Ordinal));
    }

    [Fact]
    public void Action_AtColumnLimit_Passes()
    {
        var entry = ValidEntry() with { Action = new string('A', AuditEntry.MaxActionLength) };

        Assert.True(AuditEntryValidator.TryValidate(entry, out _));
    }

    [Fact]
    public void Action_OverColumnLimit_Fails()
    {
        var entry = ValidEntry() with { Action = new string('A', AuditEntry.MaxActionLength + 1) };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains($"at most {AuditEntry.MaxActionLength}", StringComparison.Ordinal));
    }

    // ---- EntityType: varchar(50) ----------------------------------------------

    [Fact]
    public void EntityType_Blank_Fails()
    {
        var entry = ValidEntry() with { EntityType = "" };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains("EntityType is required", StringComparison.Ordinal));
    }

    [Fact]
    public void EntityType_OverColumnLimit_Fails()
    {
        var entry = ValidEntry() with { EntityType = new string('E', AuditEntry.MaxEntityTypeLength + 1) };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains($"at most {AuditEntry.MaxEntityTypeLength}", StringComparison.Ordinal));
    }

    // ---- DetailsJson: CK_Audit_Details runs ISJSON(Details) = 1 ---------------

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{"unterminated":""")]
    [InlineData("just text")]
    public void Details_MalformedJson_Fails(string details)
    {
        var entry = ValidEntry() with { DetailsJson = details };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains("well-formed JSON", StringComparison.Ordinal));
    }

    [Fact]
    public void Details_Null_Passes()
    {
        var entry = ValidEntry() with { DetailsJson = null };

        Assert.True(AuditEntryValidator.TryValidate(entry, out _));
    }

    // ---- Failure entries: a known actor is optional, but one of the two is not --

    [Fact]
    public void Failure_WithKnownActor_Passes()
    {
        var entry = new AuditFailureEntry
        {
            ActorUserId = 7,
            Action = "EXECUTE_TRANSFER",
            EntityType = "TransferRequest",
            DetailsJson = """{"reason":"concurrency conflict"}"""
        };

        Assert.True(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void Failure_WithUnknownUsernameOnly_Passes()
    {
        var entry = new AuditFailureEntry
        {
            AttemptedUsername = "unknown.officer",
            Action = "LOGIN",
            EntityType = "UserAccount"
        };

        Assert.True(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Empty(errors);
    }

    [Fact]
    public void Failure_WithNeitherActorNorUsername_Fails()
    {
        var entry = new AuditFailureEntry
        {
            Action = "LOGIN",
            EntityType = "UserAccount"
        };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains("cannot be attributed", StringComparison.Ordinal));
    }

    [Fact]
    public void Failure_UsernameOverColumnLimit_Fails()
    {
        var entry = new AuditFailureEntry
        {
            AttemptedUsername = new string('u', AuditFailureEntry.MaxUsernameLength + 1),
            Action = "LOGIN",
            EntityType = "UserAccount"
        };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Contains(errors, e => e.Contains($"at most {AuditFailureEntry.MaxUsernameLength}", StringComparison.Ordinal));
    }

    [Fact]
    public void NullEntries_Fail()
    {
        Assert.False(AuditEntryValidator.TryValidate((AuditEntry?)null, out _));
        Assert.False(AuditEntryValidator.TryValidate((AuditFailureEntry?)null, out _));
    }

    [Fact]
    public void Errors_AreReportedTogether_NotOneAtATime()
    {
        // A caller should be able to fix everything in one pass.
        var entry = new AuditEntry
        {
            ActorUserId = 0,
            Action = new string('A', AuditEntry.MaxActionLength + 1),
            EntityType = "",
            DetailsJson = "not json"
        };

        Assert.False(AuditEntryValidator.TryValidate(entry, out var errors));
        Assert.Equal(4, errors.Count);
    }
}
