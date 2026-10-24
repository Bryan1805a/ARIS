using Aris.Domain.Entities;

namespace Aris.Application.Accounts;

public interface IAccountService
{
    Task<List<UserAccount>> GetAllAccountsAsync();
    Task<bool> CreateAccountAsync(
    string username,
    string fullName,
    string password,
    string role);
}