using Aris.Application.Common;

namespace Aris.Application.Accounts;

/// <summary>
/// Seeds an idempotent, demo-sized dataset so the application has something to show
/// before the citizen/household slices are complete (<c>T11</c>).
/// </summary>
/// <remarks>
/// The seed is a development convenience, not a business use case. It still respects the
/// append-only tables and the Golden Invariant, and it refuses to run twice. It creates
/// no accounts: the administrator must create officers through account management.
/// </remarks>
public interface IDemoDataService
{
    Task<Result<DemoSeedSummary>> SeedAsync(
        int currentUserId,
        CancellationToken cancellationToken = default);
}
