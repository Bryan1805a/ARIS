namespace Aris.Application.Reports;

public interface IReportService
{
    Task<List<ResidenceStatisticsDto>> GetResidenceStatisticsAsync();

    Task<List<HouseholdStatisticsDto>> GetHouseholdStatisticsAsync();

    Task<List<ResidenceMovementDto>> GetResidenceMovementAsync(
        DateOnly fromDate,
        DateOnly toDate);
}