
using Aris.Domain.Common;

namespace Aris.Domain.Entities;

public class UserAccount : EntityBase
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Officer";

    public string Status { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; }
}
