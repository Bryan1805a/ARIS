namespace Aris.Application.Auditing;

/// <summary>Filters for the audit viewer (<c>AUD-01</c>).</summary>
public sealed record AuditSearchCriteria {
    /// <summary>Inclusive lower bound on <c>OccurredAt</c>, in UTC.</summary>
    public DateTime? FromUtc { get; init; }

    /// <summary>Exclusive upper bound on <c>OccurredAt</c>, in UTC.</summary>
    public DateTime? ToUtcExclusive { get; init; }

    /// <summary>Matches the actor's username or an attempted username (contains).</summary>
    public string? Username { get; init; }

    /// <summary>Matches <c>EntityType</c> (contains).</summary>
    public string? EntityType { get; init; }

    /// <summary>Matches <c>Action</c> (contains).</summary>
    public string? Action { get; init; }

    /// <summary><c>SUCCESS</c> or <c>FAILURE</c>; null means all.</summary>
    public string? Result { get; init; }

    /// <summary>Maximum rows returned; 1–1000.</summary>
    public int Limit { get; init; } = 500;
}