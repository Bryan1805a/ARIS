namespace Aris.Domain.Entities;

/// <summary>
/// Key/value operational parameters (<c>dbo.SystemConfiguration</c>, <c>SYS-01</c>,
/// <c>D11</c>).
/// </summary>
/// <remarks>
/// This table has no <c>rowversion</c> and therefore does not derive from
/// <see cref="Common.EntityBase"/>.
/// </remarks>
public sealed class SystemConfiguration
{
    public string ConfigKey { get; set; } = null!;

    public string ConfigValue { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }
}
