namespace Aris.Application.Accounts.Dtos;

/// <summary>Read model for the account-administration grid (<c>ACC-01</c>).</summary>
public sealed record AccountDto {
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required string FullName { get; init; }
    public required string Role { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
}