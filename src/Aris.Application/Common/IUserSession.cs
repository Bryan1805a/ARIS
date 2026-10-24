namespace Aris.Application.Common;

public interface IUserSession
{
    int? UserId { get; }
    string? Username { get; }
    string? Role { get; }

    bool IsAuthenticated { get; }

    void SignIn(int userId, string username, string role);
    void SignOut();
}