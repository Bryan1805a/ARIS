
using Aris.Application.Common;

namespace Aris.Application.Authentication;

public interface IAuthService
{
    Task<Result<LoginResult>> LoginAsync(
        string username,
        string password);
}
