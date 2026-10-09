namespace Aris.Domain.Common;

/// <summary>
/// Base type for persisted domain entities.
/// </summary>
/// <remarks>
/// <see cref="RowVersion"/> backs the optimistic-concurrency requirement
/// (CON-CONC-01 / decision D14). EF Core maps it with <c>IsRowVersion()</c>.
/// </remarks>
public abstract class EntityBase
{
    /// <summary>SQL Server <c>rowversion</c> used for concurrency conflict detection.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
