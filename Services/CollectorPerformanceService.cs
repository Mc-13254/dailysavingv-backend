using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

public class CollectorPerformanceService : ICollectorPerformanceService
{
    private readonly AppDbContext _db;
    private const double LowSuccessRateThreshold = 60.0;
    private const int FewClientsThreshold = 5;

    public CollectorPerformanceService(AppDbContext db) => _db = db;

    // All collection figures come from Transactions where TransactionType == DAILY_COLLECTION.
    // Statut == "VALIDATED" counts as a successful collection; REVERSED/CANCELLED count
    // against the success rate. There is no explicit "MISSED" transaction concept in the
    // base schema, so "missed" here means zero collections logged for the collector that day.
    private IQueryable<Transactions> CollectionsQuery(DateTime from, DateTime to, string? collectorId = null)
    {
        var query = _db.Transactions
            .Where(t => t.TransactionType == TransactionType.DAILY_COLLECTION
                        && t.DateTransaction >= from.Date
                        && t.DateTransaction < to.Date.AddDays(1));
        if (collectorId != null) query = query.Where(t => t.CollectorID == collectorId);
        return query;
    }

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(PerformanceFilter filter)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todays = await CollectionsQuery(today, today).ToListAsync();
        var monthly = await CollectionsQuery(monthStart, today).ToListAsync();

        var totalCollectors = await _db.Collectors.CountAsync();
        var activeToday = todays.Select(t => t.CollectorID).Distinct().Count();

        var successCount = monthly.Count(t => t.Statut == "VALIDATED");
        var successRate = monthly.Count > 0 ? successCount * 100.0 / monthly.Count : 0;

        return new DashboardKpiDto(
            totalCollectors, activeToday, Math.Max(0, totalCollectors - activeToday),
            todays.Sum(t => t.Montant), monthly.Sum(t => t.Montant), monthly.Sum(t => t.MontantCommission),
            Math.Round(successRate, 1),
            totalCollectors > 0 ? Math.Round(monthly.Sum(t => t.Montant) / totalCollectors, 2) : 0
        );
    }

    public async Task<List<CollectorPerformanceRowDto>> GetPerformanceTableAsync(PerformanceFilter filter)
    {
        var collectorsQuery = _db.Collectors.Include(c => c.Agence).Include(c => c.ZoneCollecte).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.CollectorID)) collectorsQuery = collectorsQuery.Where(c => c.CollectorID == filter.CollectorID);
        if (filter.AgenceID.HasValue) collectorsQuery = collectorsQuery.Where(c => c.AgenceID == filter.AgenceID.Value);
        if (filter.DepartmentID.HasValue) collectorsQuery = collectorsQuery.Where(c => c.DepartmentID == filter.DepartmentID.Value);
        if (filter.ZoneCollecteID.HasValue) collectorsQuery = collectorsQuery.Where(c => c.ZoneCollecteID == filter.ZoneCollecteID.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) collectorsQuery = collectorsQuery.Where(c => c.CDETAT == filter.Status);

        var collectors = await collectorsQuery.ToListAsync();
        var rows = new List<CollectorPerformanceRowDto>();
        foreach (var c in collectors)
            rows.Add(await BuildRowAsync(c, filter));

        return rows;
    }

    private async Task<CollectorPerformanceRowDto> BuildRowAsync(Collector c, PerformanceFilter filter)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todays = await CollectionsQuery(today, today, c.CollectorID).ToListAsync();
        var monthly = await CollectionsQuery(monthStart, today, c.CollectorID).ToListAsync();
        var target = await _db.CollectorTargets.FirstOrDefaultAsync(
            t => t.CollectorID == c.CollectorID && t.PeriodType == "MONTHLY" && t.PeriodStart == monthStart);

        var assignedClients = await _db.Clients.CountAsync(cl => cl.CollectorID == c.CollectorID);
        var visitedToday = todays.Select(t => t.ClientID).Distinct().Count();

        var successCount = monthly.Count(t => t.Statut == "VALIDATED");
        var successRate = monthly.Count > 0 ? successCount * 100.0 / monthly.Count : 0;
        var achievement = (target != null && target.TargetAmount > 0)
            ? (double)(monthly.Sum(t => t.Montant) / target.TargetAmount * 100)
            : 0;

        return new CollectorPerformanceRowDto(
            c.CollectorID, c.CollectorID, $"{c.Name} {c.Surname}".Trim(), null,
            c.Agence?.Nom, c.ZoneCollecte?.Libelle, assignedClients, visitedToday,
            todays.Count, monthly.Count, todays.Sum(t => t.Montant), monthly.Sum(t => t.Montant),
            monthly.Sum(t => t.MontantCommission), Math.Round(achievement, 1), Math.Round(successRate, 1), c.CDETAT
        );
    }

    public async Task<CollectorPerformanceDetailDto?> GetCollectorDetailAsync(string collectorId, PerformanceFilter filter)
    {
        var collector = await _db.Collectors.Include(c => c.Agence).Include(c => c.ZoneCollecte)
            .Include(c => c.Supervisor)
            .FirstOrDefaultAsync(c => c.CollectorID == collectorId);
        if (collector == null) return null;

        var row = await BuildRowAsync(collector, filter);

        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var weekly = await CollectionsQuery(weekStart, today, collectorId).ToListAsync();
        var monthly = await CollectionsQuery(monthStart, today, collectorId).ToListAsync();
        var allTime = await CollectionsQuery(new DateTime(2000, 1, 1), today, collectorId).ToListAsync();

        var dailyTarget = await _db.CollectorTargets.FirstOrDefaultAsync(t => t.CollectorID == collectorId && t.PeriodType == "DAILY" && t.PeriodStart == today);
        var weeklyTarget = await _db.CollectorTargets.FirstOrDefaultAsync(t => t.CollectorID == collectorId && t.PeriodType == "WEEKLY" && t.PeriodStart == weekStart);
        var monthlyTarget = await _db.CollectorTargets.FirstOrDefaultAsync(t => t.CollectorID == collectorId && t.PeriodType == "MONTHLY" && t.PeriodStart == monthStart);

        var zoneNames = await _db.CollectorZoneAssignments
            .Include(a => a.ZoneCollecte)
            .Where(a => a.CollectorID == collectorId && a.Status == "ACTIVE")
            .Select(a => a.ZoneCollecte!.Libelle ?? "")
            .ToListAsync();

        var activeClients = await _db.Clients
            .CountAsync(cl => cl.CollectorID == collectorId && cl.ValidationStatus == "VALIDATED");

        var clientRows = await _db.Clients
            .Where(cl => cl.CollectorID == collectorId)
            .Select(cl => new { cl.ClientID, cl.Nom, cl.Prenom, cl.ValidationStatus })
            .ToListAsync();

        var clientPerf = new List<ClientPerformanceDto>();
        foreach (var cl in clientRows)
        {
            var lastCollection = allTime.Where(t => t.ClientID == cl.ClientID)
                .OrderByDescending(t => t.DateTransaction).FirstOrDefault();
            var totalSavings = await _db.Accounts
                .Where(a => a.ClientID == cl.ClientID && a.Active)
                .SumAsync(a => (decimal?)a.Balance) ?? 0m;

            clientPerf.Add(new ClientPerformanceDto(
                cl.ClientID, $"{cl.Nom} {cl.Prenom}".Trim(),
                lastCollection?.DateTransaction, lastCollection?.Montant, totalSavings, cl.ValidationStatus
            ));
        }

        var zonePerf = new List<ZonePerformanceDto>();
        var zoneGroups = await _db.CollectorZoneAssignments
            .Include(a => a.ZoneCollecte)
            .Where(a => a.CollectorID == collectorId && a.Status == "ACTIVE")
            .ToListAsync();
        foreach (var z in zoneGroups)
        {
            var zoneClients = await _db.Clients.CountAsync(cl => cl.ZoneCollecteID == z.ZoneCollecteID);
            var zoneCollections = monthly.Count(t => clientRows.Any(cl => cl.ClientID == t.ClientID));
            zonePerf.Add(new ZonePerformanceDto(
                z.ZoneCollecte?.Libelle ?? "", zoneClients, zoneCollections,
                zoneCollections > 0 ? Math.Round(monthly.Where(t => t.ClientID != null).Sum(t => t.Montant) / zoneCollections, 2) : 0
            ));
        }

        return new CollectorPerformanceDetailDto(
            row, collector.PhoneNumber, collector.Supervisor?.Username, zoneNames,
            activeClients, weekly.Count, weekly.Sum(t => t.Montant),
            allTime.Sum(t => t.Montant),
            allTime.Count > 0 ? Math.Round(allTime.Average(t => t.Montant), 2) : 0,
            allTime.Count > 0 ? allTime.Max(t => t.Montant) : 0,
            allTime.Count > 0 ? allTime.Min(t => t.Montant) : 0,
            monthly.Count(t => t.Statut is "REVERSED" or "CANCELLED"),
            monthly.Count > 0 ? Math.Round(monthly.Sum(t => t.Montant) / today.Day, 2) : 0,
            new TargetProgressDto(dailyTarget?.TargetAmount ?? 0, (await CollectionsQuery(today, today, collectorId).ToListAsync()).Sum(t => t.Montant)),
            new TargetProgressDto(weeklyTarget?.TargetAmount ?? 0, weekly.Sum(t => t.Montant)),
            new TargetProgressDto(monthlyTarget?.TargetAmount ?? 0, monthly.Sum(t => t.Montant)),
            clientPerf, zonePerf
        );
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(PerformanceFilter filter, int top)
    {
        var rows = await GetPerformanceTableAsync(filter);
        return rows
            .OrderByDescending(r => r.CommissionEarned)
            .Take(top)
            .Select((r, i) => new LeaderboardEntryDto(
                i + 1, r.PhotoUrl, r.FullName, r.CollectionsThisMonth, r.CommissionEarned,
                r.TargetAchievementPercent, r.CollectionSuccessPercent))
            .ToList();
    }

    public async Task<List<BottomPerformerDto>> GetBottomPerformersAsync(PerformanceFilter filter)
    {
        var rows = await GetPerformanceTableAsync(filter);
        var result = new List<BottomPerformerDto>();

        foreach (var r in rows)
        {
            var reasons = new List<string>();
            if (r.TargetAchievementPercent < 50) reasons.Add("Low Collection");
            if (r.AssignedClients < FewClientsThreshold) reasons.Add("Few Active Clients");
            if (r.CollectionSuccessPercent < LowSuccessRateThreshold) reasons.Add("Low Success Rate");

            if (reasons.Count > 0) result.Add(new BottomPerformerDto(r.FullName, reasons));
        }
        return result.OrderBy(r => r.Reasons.Count).ToList();
    }

    public async Task<List<PerformanceAlertDto>> GetAlertsAsync()
    {
        var rows = await GetPerformanceTableAsync(new PerformanceFilter(null, null, null, null, null, null, null));
        var alerts = new List<PerformanceAlertDto>();

        foreach (var r in rows)
        {
            if (r.CollectionsToday == 0)
                alerts.Add(new PerformanceAlertDto(r.FullName, "NoCollectionToday",
                    $"{r.FullName} n'a effectué aucune collecte aujourd'hui.", "WARNING"));

            if (r.TargetAchievementPercent < 50)
                alerts.Add(new PerformanceAlertDto(r.FullName, "BelowMonthlyTarget",
                    $"{r.FullName} est en dessous de 50% de son objectif mensuel.", "CRITICAL"));

            if (r.Status != "ACTIVE")
                alerts.Add(new PerformanceAlertDto(r.FullName, "Inactive",
                    $"{r.FullName} est actuellement {r.Status}.", "INFO"));
        }
        return alerts;
    }

    public async Task<List<ChartSeriesDto>> GetChartsAsync(string chartType, PerformanceFilter filter)
    {
        var from = filter.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = filter.DateTo ?? DateTime.UtcNow.Date;
        var collections = await CollectionsQuery(from, to, filter.CollectorID).ToListAsync();

        var points = collections
            .GroupBy(t => t.DateTransaction.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new ChartPointDto(g.Key, chartType == "Commission" ? g.Sum(t => t.MontantCommission) : g.Sum(t => t.Montant)))
            .ToList();

        return new List<ChartSeriesDto> { new(chartType, points) };
    }
}
