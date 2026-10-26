namespace Aris.Application.Reports;

public sealed class HouseholdStatisticsDto
{
    public int TotalHouseholds { get; set; }

    public int ActiveHouseholds { get; set; }

    public int InactiveHouseholds { get; set; }

    public double AverageMembers { get; set; }
}