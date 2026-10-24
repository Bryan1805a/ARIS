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
    string role)
    {
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
}