using Aris.Application.Reports;

namespace Aris.Application.Interfaces;

public interface IReportRepository
{
    Task<List<ResidenceStatisticsDto>> GetResidenceStatisticsAsync();

    Task<List<HouseholdStatisticsDto>> GetHouseholdStatisticsAsync();
    Task<List<ResidenceMovementDto>> GetResidenceMovementAsync(
    DateOnly fromDate,
    DateOnly toDate);
}