using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aris.UnitTests.Infrastructure;

/// <summary>
/// Base class for tests that need an EF Core context but no real database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Use this for:</b> business logic, validation, mapping defaults, and "did we
/// write the right rows" assertions. It runs in milliseconds and needs nothing
/// installed, so unit tests always run.
/// </para>
/// <para>
/// <b>Do NOT use this to prove database rules.</b> An in-memory provider has no
/// triggers, no CHECK constraints, no filtered unique indexes, no computed columns
/// and no <c>rowversion</c> generation. It will happily accept data that
/// <c>ARIS-DB</c> rejects. The golden invariant, append-only guards, the
/// "at most one ACTIVE membership per citizen" index and temporal-overlap rules exist
/// only in SQL Server - those are covered by <c>tests/Aris.Tests</c> against the real
/// database. Do not "fix" a constraint here by adding it to C#; that would hide the
/// discrepancy instead of catching it.
/// </para>
/// <para>
/// Derive from this and override <see cref="CreateContext"/> once
/// <c>ArisDbContext</c> exists (P1-DB2):
/// <code>
/// public sealed class HouseholdServiceTests : InMemoryDatabaseTestBase&lt;ArisDbContext&gt;
/// {
///     protected override ArisDbContext CreateContext(DbContextOptions&lt;ArisDbContext&gt; options)
///         => new(options);
///
///     [Fact]
///     public async Task Registers_a_household() { /* use Context */ }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class InMemoryDatabaseTestBase<TContext> : IAsyncLifetime
    where TContext : DbContext
{
    private DbContextOptions<TContext> _options = null!;

    /// <summary>The context under test. A fresh instance per call, sharing one store.</summary>
    protected TContext Context => CreateContext(_options);

    /// <summary>
    /// A distinct store per test class instance, so parallel test classes cannot see
    /// each other's rows.
    /// </summary>
    protected string DatabaseName { get; } = $"aris-unit-{Guid.NewGuid():N}";

    /// <summary>Constructs the concrete context from the configured options.</summary>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    public async ValueTask InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(DatabaseName)
            .EnableSensitiveDataLogging()
            .Options;

        // Create the model once so a mapping error fails here, clearly, rather than
        // inside the first assertion of a test.
        await using var context = CreateContext(_options);
        await context.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
