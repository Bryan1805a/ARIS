namespace Aris.Application.Developer;

/// <summary>
/// Input for the local developer-mode account creator. This is not a business
/// use case: it deliberately bypasses the authenticated administrator check so a
/// fresh local database can be brought up without using the bootstrap screen.
/// </summary>
public sealed record CreateDeveloperAccountDto {
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string Password { get; init; }

    /// <summary><c>ADMIN</c> or <c>OFFICER</c>.</summary>
    public required string Role { get; init; }
}
