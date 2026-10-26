using Aris.Application.Interfaces;
using Aris.Application.Reports;
using Aris.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Aris.Infrastructure.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly IDbContextFactory<ArisDbContext> _contextFactory;

    public ReportRepository(
        IDbContextFactory<ArisDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ResidenceStatisticsDto>> GetResidenceStatisticsAsync()
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var statistics = await (
            from residence in context.Residences.AsNoTracking()
            join address in context.Addresses.AsNoTracking()
                on residence.AddressId equals address.AddressId
            where residence.Status == "ACTIVE"
                  && residence.EndDate == null
            group residence by new
            {
                address.Province,
                address.Ward
            }
            into grouped
            orderby grouped.Key.Province, grouped.Key.Ward
            select new ResidenceStatisticsDto
            {
                Province = grouped.Key.Province,
                Ward = grouped.Key.Ward,
                TotalResidents = grouped.Count()
            }
        ).ToListAsync();

        return statistics;
    }

    public async Task<List<HouseholdStatisticsDto>> GetHouseholdStatisticsAsync()
    {
        await using var context =
            await _contextFactory.CreateDbContextAsync();

        int totalHouseholds = await context.Households
            .AsNoTracking()
            .CountAsync();

        int activeHouseholds = await context.Households
            .AsNoTracking()
            .CountAsync(h => h.Status == "ACTIVE");

        int inactiveHouseholds = await context.Households
            .AsNoTracking()
            .CountAsync(h => h.Status == "INACTIVE");

        int totalMembers = await context.HouseholdMemberships
            .AsNoTracking()
            .CountAsync(m =>
                m.Status == "ACTIVE" &&
                m.EndDate == null);

        double averageMembers = totalHouseholds > 0
            ? Math.Round((double)totalMembers / totalHouseholds, 2)
            : 0;

        return new List<HouseholdStatisticsDto>
    {
        new HouseholdStatisticsDto
        {
            TotalHouseholds = totalHouseholds,
            ActiveHouseholds = activeHouseholds,
            InactiveHouseholds = inactiveHouseholds,
            AverageMembers = averageMembers
        }
    };
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

        await using var context =
            await _contextFactory.CreateDbContextAsync();

        var newRegistrations = await context.Residences
            .AsNoTracking()
            .Where(r =>
                r.Status != "VOID" &&
                r.OpenedByRequestId == null &&
                r.StartDate >= fromDate &&
                r.StartDate <= toDate)
            .GroupBy(r => r.StartDate)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var transfers = await context.TransferRequests
            .AsNoTracking()
            .Where(t =>
                t.Status == "APPROVED" &&
                t.EffectiveDate >= fromDate &&
                t.EffectiveDate <= toDate)
            .GroupBy(t => t.EffectiveDate)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var endedResidences = await context.Residences
            .AsNoTracking()
            .Where(r =>
                r.Status == "ENDED" &&
                r.EndDate != null &&
                r.EndDate >= fromDate &&
                r.EndDate <= toDate)
            .GroupBy(r => r.EndDate!.Value)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var dates = newRegistrations.Select(x => x.Date)
            .Concat(transfers.Select(x => x.Date))
            .Concat(endedResidences.Select(x => x.Date))
            .Distinct()
            .OrderBy(d => d);

        return dates.Select(date => new ResidenceMovementDto
        {
            ReportDate = date,
            NewRegistrations = newRegistrations
                .Where(x => x.Date == date)
                .Sum(x => x.Count),
            Transfers = transfers
                .Where(x => x.Date == date)
                .Sum(x => x.Count),
            EndedResidences = endedResidences
                .Where(x => x.Date == date)
                .Sum(x => x.Count)
        }).ToList();
    }
}