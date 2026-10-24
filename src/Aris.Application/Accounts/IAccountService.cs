using Aris.Domain.Entities;

namespace Aris.Application.Accounts;

public interface IAccountService
{
    Task<List<UserAccount>> GetAllAccountsAsync();
    Task<bool> CreateAccountAsync(
    string username,
    string fullName,
    string password,
    string role,
    int currentUserId);
    Task<bool> ChangeAccountStatusAsync(
        int userId,
        string status,
        int currentUserId
    );
    Task<bool> ResetPasswordAsync(
        int userId,
        string newPassword,
        int currentUserId
);
}