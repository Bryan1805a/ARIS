
using Aris.Application.Interfaces;
using Aris.Domain.Entities;

namespace Aris.Infrastructure.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    public Task<UserAccount?> GetByUsernameAsync(string username)
    {
        // TODO: Query UserAccount from ArisDbContext.
        throw new NotImplementedException(
            "Database access is not configured yet.");
    }
}
