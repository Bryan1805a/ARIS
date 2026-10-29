namespace Aris.Application.Auditing;

public class AuditLogDto
{
    public long AuditLogId { get; set; }
    public string Username { get; set; } = "";

    public string Action { get; set; } = "";

    public string EntityType { get; set; } = "";

    public int? EntityId { get; set; }

    public DateTime OccurredAt { get; set; }

    public string Result { get; set; } = "";

    public string? Details { get; set; }
}