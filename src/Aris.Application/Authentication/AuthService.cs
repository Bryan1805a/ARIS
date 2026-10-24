
using Aris.Application.Common;
using Aris.Application.Interfaces;

namespace Aris.Application.Authentication;

public class AuthService : BaseService, IAuthService
{
    private readonly IUserAccountRepository _userAccountRepository;

    public AuthService(IUserAccountRepository userAccountRepository)
    {
        _userAccountRepository = userAccountRepository;
    }

    public async Task<Result<LoginResult>> LoginAsync(
        string username,
        string password)
    {
        var account = await _userAccountRepository
            .GetByUsernameAsync(username);

        if (account == null)
        {
            return Failure<LoginResult>(
                "Invalid username or password.");
        }

        if (account.Status != "ACTIVE")
        {
            return Failure<LoginResult>(
                "This account is inactive.");
        }


        bool isPasswordValid;

        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(
                password,
                account.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return Failure<LoginResult>(
                "Authentication failed. Please contact the administrator.");
        }

        if (!isPasswordValid)
        {
            return Failure<LoginResult>(
                "Invalid username or password.");
        }

        return Success(new LoginResult
        {
            UserId = account.UserId,
            Username = account.Username,
            Role = account.Role
        });
    }
}
