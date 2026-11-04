using Aris.Application.Accounts;
using Aris.Application.Common;

namespace Aris.Application.Developer;

/// <summary>
/// Local developer conveniences exposed by the login screen's Developer mode.
/// </summary>
/// <remarks>
/// This service is intentionally privileged: it can create an account without an
/// authenticated administrator and can seed the demo dataset. It exists for the
/// academic prototype only and must not be exposed by a production client.
/// </remarks>
public interface IDeveloperService {
    /// <summary>
    /// Creates an active <c>ADMIN</c> or <c>OFFICER</c> account. The password is
    /// hashed; the account is ready to sign in immediately.
    /// </summary>
    Task<Result<int>> CreateAccountAsync(
        CreateDeveloperAccountDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Seeds the standard demo dataset using the most recently created active
    /// administrator as the audit actor. Fails when no active administrator exists.
    /// </summary>
    Task<Result<DemoSeedSummary>> SeedDemoDataAsync(
        CancellationToken ct = default);
}
