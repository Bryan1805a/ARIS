using Aris.Application.Common;

namespace Aris.Application.Accounts;

/// <summary>
/// Creates the first administrator on a fresh database (<c>R15</c>, <c>T6</c>).
/// </summary>
/// <remarks>
/// Only the application can hash a password (<c>D10</c>), so the first account cannot be
/// seeded by the DDL. This service is deliberately separate from
/// <see cref="IAccountService"/>: account administration requires an authenticated
/// <c>ADMIN</c>, which does not exist yet at bootstrap time.
/// </remarks>
public interface IBootstrapService
{
    /// <summary>True when at least one account exists; used to show or hide the bootstrap screen.</summary>
    Task<bool> HasAnyAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the first <c>ADMIN</c>. Fails if any account already exists, so the
    /// endpoint cannot be used to create a second administrator without authentication.
    /// </summary>
    Task<Result<int>> CreateFirstAdminAsync(
        string username,
        string fullName,
        string password,
        CancellationToken cancellationToken = default);
}
