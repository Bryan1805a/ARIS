namespace Aris.Application.Authentication;

public class LoginResult
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool MustChangePassword { get; set; }
}