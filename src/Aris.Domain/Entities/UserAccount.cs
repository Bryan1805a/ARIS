using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Local login, role, and lockout state (<c>dbo.UserAccount</c>, <c>D10</c>).
/// </summary>
/// <remarks>
/// <see cref="PasswordHash"/> stores a salted PBKDF2/BCrypt hash only
/// (<c>CON-SEC-01</c>); a password is never persisted or audited.
/// </remarks>
public sealed class UserAccount : EntityBase
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    /// <summary><c>OFFICER</c> or <c>ADMIN</c>.</summary>
    public string Role { get; set; } = null!;

    /// <summary><c>ACTIVE</c> or <c>LOCKED</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public bool MustChangePassword { get; set; } = true;

    public DateTime? PasswordChangedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
