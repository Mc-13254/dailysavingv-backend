namespace DailySavingV.API.DTOs;

public record PerformanceFilter(
    DateTime? DateFrom, DateTime? DateTo, string? CollectorID,
    int? AgenceID, int? DepartmentID, int? ZoneCollecteID, string? Status
);

public record DashboardKpiDto(
    int TotalCollectors, int ActiveCollectorsToday, int InactiveCollectors,
    decimal TodaysCollections, decimal MonthlyCollections, decimal TotalCommission,
    double CollectionSuccessRate, decimal AverageCollectionPerCollector
);

public record CollectorPerformanceRowDto(
    string CollectorID, string CollectorCode, string FullName, string? PhotoUrl,
    string? Agency, string? Zone, int AssignedClients, int ClientsVisitedToday,
    int CollectionsToday, int CollectionsThisMonth, decimal TodaysAmount, decimal MonthlyAmount,
    decimal CommissionEarned, double TargetAchievementPercent, double CollectionSuccessPercent, string Status
);

public record TargetProgressDto(decimal TargetAmount, decimal CollectedAmount)
{
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CollectedAmount);
    public double AchievementPercent => TargetAmount > 0 ? (double)(CollectedAmount / TargetAmount * 100) : 0;
    public string Status => AchievementPercent >= 100 ? "Completed" : AchievementPercent >= 50 ? "InProgress" : "BelowTarget";
}

public record ClientPerformanceDto(
    string ClientID, string ClientName, DateTime? LastCollectionDate,
    decimal? LastCollectionAmount, decimal TotalSavings, string Status
);

public record ZonePerformanceDto(string ZoneLibelle, int TotalClients, int TotalCollections, decimal AverageCollection);

public record CollectorPerformanceDetailDto(
    CollectorPerformanceRowDto Summary, string? Phone, string? Supervisor, List<string> AssignedZones,
    int ActiveClients, int WeeklyCollections, decimal WeeklyAmount, decimal TotalAmountCollected,
    decimal AverageCollection, decimal HighestCollection, decimal LowestCollection,
    int MissedCollections, decimal AverageDailyCollection,
    TargetProgressDto DailyTarget, TargetProgressDto WeeklyTarget, TargetProgressDto MonthlyTarget,
    List<ClientPerformanceDto> Clients, List<ZonePerformanceDto> Zones
);

public record LeaderboardEntryDto(
    int Rank, string? PhotoUrl, string CollectorName, int Collections,
    decimal Commission, double AchievementPercent, double SuccessRatePercent
);

public record BottomPerformerDto(string CollectorName, List<string> Reasons);

public record PerformanceAlertDto(string CollectorName, string AlertType, string Message, string Severity);

public record ChartPointDto(string X, decimal Y);
public record ChartSeriesDto(string Label, List<ChartPointDto> Points);
