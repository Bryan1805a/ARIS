using Aris.Domain.Common;

namespace Aris.Domain.Entities;

/// <summary>
/// Registration of temporary residence for a citizen at an address (<c>dbo.TemporaryResidence</c>).
/// </summary>
public sealed class TemporaryResidence : EntityBase
{
    public int TempResId { get; set; }

    public int CitizenId { get; set; }

    public int AddressId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    /// <summary>Lý do tạm trú (thuê trọ, làm việc, đi học, chữa bệnh, ở nhờ...).</summary>
    public string StayReason { get; set; } = string.Empty;

    /// <summary>Thông tin chủ hộ / người bảo lãnh nơi tạm trú.</summary>
    public string? HostInfo { get; set; }

    /// <summary><c>ACTIVE</c>, <c>EXPIRED</c>, or <c>CANCELLED</c>.</summary>
    public string Status { get; set; } = "ACTIVE";

    public string? Notes { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? EndedBy { get; set; }

    public DateTime? EndedAt { get; set; }
}

