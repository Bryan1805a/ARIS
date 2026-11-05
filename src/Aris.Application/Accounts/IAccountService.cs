using Aris.Application.Accounts.Dtos;
using Aris.Application.Common;

namespace Aris.Application.Accounts;

/// <summary>
/// Account administration and self-service password change (<c>ACC-01</c>, <c>AUTH-01</c>).
/// </summary>
public interface IAccountService {
    Task<Result<List<AccountDto>>> GetAllAccountsAsync(
        int currentUserId,
        CancellationToken ct = default);

    Task<Result<int>> CreateAccountAsync(
        string username,
        string fullName,
        string password,
        string role,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> ChangeAccountStatusAsync(
        int userId,
        string status,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(
        int userId,
        string newPassword,
        int currentUserId,
        CancellationToken ct = default);

    Task<Result> ChangeOwnPasswordAsync(
        int userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    Task<Result> UpdateAccountAsync(
    int userId,
    string username,
    string role,
    int currentUserId,
    CancellationToken ct = default);
}