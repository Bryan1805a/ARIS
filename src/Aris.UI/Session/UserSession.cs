using Aris.Application.Common;

namespace Aris.UI.Session;

public class UserSession : IUserSession
{
    public int? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;

    public void SignIn(int userId, string username, string role)
    {
        UserId = userId;
        Username = username;
        Role = role;
    }

    public void SignOut()
    {
        UserId = null;
        Username = null;
        Role = null;
    }
}