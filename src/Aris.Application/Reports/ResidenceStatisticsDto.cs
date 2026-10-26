namespace Aris.Application.Reports;

public sealed class ResidenceStatisticsDto
{
    public string Province { get; set; } = string.Empty;

    public string Ward { get; set; } = string.Empty;

    public int TotalResidents { get; set; }
}