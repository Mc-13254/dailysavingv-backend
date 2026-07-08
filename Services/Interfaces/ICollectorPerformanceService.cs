using DailySavingV.API.DTOs;

namespace DailySavingV.API.Services.Interfaces;

public interface ICollectorPerformanceService
{
    Task<DashboardKpiDto> GetDashboardKpisAsync(PerformanceFilter filter);
    Task<List<CollectorPerformanceRowDto>> GetPerformanceTableAsync(PerformanceFilter filter);
    Task<CollectorPerformanceDetailDto?> GetCollectorDetailAsync(string collectorId, PerformanceFilter filter);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(PerformanceFilter filter, int top);
    Task<List<BottomPerformerDto>> GetBottomPerformersAsync(PerformanceFilter filter);
    Task<List<PerformanceAlertDto>> GetAlertsAsync();
    Task<List<ChartSeriesDto>> GetChartsAsync(string chartType, PerformanceFilter filter);
}
