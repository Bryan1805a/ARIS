using Aris.Application.Interfaces;

namespace Aris.Application.Reports;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<List<ResidenceStatisticsDto>> GetResidenceStatisticsAsync()
    {
        return await _reportRepository.GetResidenceStatisticsAsync();
    }

    public async Task<List<HouseholdStatisticsDto>> GetHouseholdStatisticsAsync()
    {
        return await _reportRepository.GetHouseholdStatisticsAsync();
    }

    public async Task<List<ResidenceMovementDto>> GetResidenceMovementAsync(
    DateOnly fromDate,
    DateOnly toDate)
    {
        if (fromDate > toDate)
        {
            throw new ArgumentException(
                "From date must not be later than to date.");
        }

        return await _reportRepository.GetResidenceMovementAsync(
            fromDate,
            toDate);
    }
}