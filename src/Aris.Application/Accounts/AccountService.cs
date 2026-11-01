using Aris.Application.Interfaces;
using Aris.Domain.Entities;

namespace Aris.Application.Accounts;

// Manages officer and admin accounts (create, activate/lock, password reset)
public class AccountService : IAccountService
{
    private readonly IUserAccountRepository _userAccountRepository;

    public AccountService(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    // Fetches all accounts (only admins are allowed to view this)
    public async Task<List<UserAccount>> GetAllAccountsAsync(int currentUserId)
    {
        // 1. Verify that the requesting user is an active admin
        var currentUser = await _userAccountRepository
            .GetByIdAsync(currentUserId);

        if (currentUser == null ||
            currentUser.Role != "ADMIN" ||
            currentUser.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to view accounts.");
        }

        return await _userAccountRepository.GetAllAsync();
    }

    // Creates a new officer or admin account
    public async Task<bool> CreateAccountAsync(
        string username,
        string fullName,
        string password,
        string role,
        int currentUserId)
    {
        // 1. Security check: make sure caller is an active admin
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

        // 2. Basic input validation
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

        // 3. Make sure username is unique
        var existingAccount = await _userAccountRepository
            .GetByUsernameAsync(username);

        if (existingAccount != null)
        {
            return false;
        }

        // 4. Create the account with a BCrypt hashed password
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

    // Changes account status between ACTIVE and LOCKED
    public async Task<bool> ChangeAccountStatusAsync(
        int userId,
        string status,
        int currentUserId)
    {
        // 1. Caller must be an active admin
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

        // 2. Prevent admin from locking their own account by accident!
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

    // Admin resets another user's password (forces user to change password on next login)
    public async Task<bool> ResetPasswordAsync(
        int userId,
        string newPassword,
        int currentUserId)
    {
        // 1. Caller must be an active admin
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

        // 2. Hash new password and flag MustChangePassword so they're forced to pick a new one
        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        account.MustChangePassword = true;
        account.PasswordChangedAt = null;

        await _userAccountRepository.UpdateAsync(account);

        return true;
    }

    // Allows a user to change their own password (usually right after first login or reset)
    public async Task<bool> ChangeOwnPasswordAsync(
        int userId,
        string currentPassword,
        string newPassword)
    {
        var account = await _userAccountRepository.GetByIdAsync(userId);

        if (account == null || account.Status != "ACTIVE")
        {
            throw new UnauthorizedAccessException(
                "Account is not available.");
        }

        // Only allowed if MustChangePassword flag is true
        if (!account.MustChangePassword)
        {
            throw new InvalidOperationException(
                "Password change is not required.");
        }

        // Verify the old password first
        bool isCurrentPasswordValid;

        try
        {
            isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                currentPassword,
                account.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            throw new InvalidOperationException(
                "Unable to verify the current password.");
        }

        if (!isCurrentPasswordValid)
        {
            throw new ArgumentException(
                "Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 8)
        {
            throw new ArgumentException(
                "Password must contain at least 8 characters.");
        }

        // Disallow reusing the exact same password
        if (BCrypt.Net.BCrypt.Verify(
            newPassword,
            account.PasswordHash))
        {
            throw new ArgumentException(
                "New password must be different from the current password.");
        }

        // Save new password and clear the change requirement
        account.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(newPassword);

        account.MustChangePassword = false;
        account.PasswordChangedAt = DateTime.UtcNow;

        await _userAccountRepository.UpdateAsync(account);

        return true;
    }
}
