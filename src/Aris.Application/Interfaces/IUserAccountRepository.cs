using Aris.Domain.Entities;

namespace Aris.Application.Interfaces;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByUsernameAsync(string username);

    Task<List<UserAccount>> GetAllAsync();
    Task AddAsync(UserAccount account);
}