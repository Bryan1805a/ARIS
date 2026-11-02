namespace Aris.Application.SystemConfiguration.Dtos;

/// <summary>Read model for the configuration editor (<c>SYS-01</c>).</summary>
public sealed record SystemConfigurationDto {
    public required string ConfigKey { get; init; }
    public required string ConfigValue { get; init; }
    public string? Description { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public int? UpdatedBy { get; init; }
}