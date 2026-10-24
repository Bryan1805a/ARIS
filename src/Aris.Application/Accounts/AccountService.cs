using Aris.Application.Interfaces;
using Aris.Domain.Entities;

namespace Aris.Application.Accounts;

public class AccountService : IAccountService
{
    private readonly IUserAccountRepository _userAccountRepository;

    public AccountService(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<List<UserAccount>> GetAllAccountsAsync()
    {
        return await _userAccountRepository.GetAllAsync();
    }
    public async Task<bool> CreateAccountAsync(
     string username,
     string fullName,
     string password,
     string role,
     int currentUserId)
    {
        var currentUser = await _userAccountRepository.GetByIdAsync(currentUserId);

        if (currentUser == null ||
            currentUser.Role != "ADMIN" ||
            currentUser.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to create accounts.");
        }
        username = username.Trim();
        fullName = fullName.Trim();
        role = role.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("All fields are required.");
        }

        if (role != "ADMIN" && role != "OFFICER")
        {
            throw new ArgumentException("Invalid account role.");
        }

        var existingAccount = await _userAccountRepository
            .GetByUsernameAsync(username);

        if (existingAccount != null)
        {
            return false;
        }

        var account = new UserAccount
        {
            Username = username,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Status = "ACTIVE"
        };

        await _userAccountRepository.AddAsync(account);

        return true;
    }
    public async Task<bool> ChangeAccountStatusAsync(
    int userId,
    string status,
    int currentUserId)
    {
        var currentUser = await _userAccountRepository.GetByIdAsync(currentUserId);

        if (currentUser == null ||
            currentUser.Role != "ADMIN" ||
            currentUser.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to manage accounts.");
        }

        status = status.Trim().ToUpperInvariant();

        if (status != "ACTIVE" && status != "LOCKED")
        {
            throw new ArgumentException("Invalid account status.");
        }

        if (userId == currentUserId && status == "LOCKED")
        {
            throw new InvalidOperationException(
                "You cannot disable your own account.");
        }

        var account = await _userAccountRepository.GetByIdAsync(userId);

        if (account == null)
        {
            return false;
        }

        account.Status = status;

        await _userAccountRepository.UpdateAsync(account);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        int userId,
        string newPassword,
        int currentUserId)
    {
        var currentUser = await _userAccountRepository
            .GetByIdAsync(currentUserId);

        if (currentUser == null ||
            currentUser.Role != "ADMIN" ||
            currentUser.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to reset passwords.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.");
        }

        var account = await _userAccountRepository.GetByIdAsync(userId);

        if (account == null)
        {
            return false;
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.MustChangePassword = true;
        account.PasswordChangedAt = null;

        await _userAccountRepository.UpdateAsync(account);

        return true;
    }
}