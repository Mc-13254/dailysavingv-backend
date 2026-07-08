using DailySavingV.API.DTOs;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/collector-performance")]
[Authorize]
public class CollectorPerformanceController : ControllerBase
{
    private readonly ICollectorPerformanceService _perfService;

    public CollectorPerformanceController(ICollectorPerformanceService perfService) => _perfService = perfService;

    private static PerformanceFilter BuildFilter(
        DateTime? dateFrom, DateTime? dateTo, string? collectorId,
        int? agenceId, int? departmentId, int? zoneCollecteId, string? status)
        => new(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status);

    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardKpiDto>> GetKpis(
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? collectorId,
        [FromQuery] int? agenceId, [FromQuery] int? departmentId, [FromQuery] int? zoneCollecteId, [FromQuery] string? status)
        => Ok(await _perfService.GetDashboardKpisAsync(BuildFilter(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status)));

    [HttpGet]
    public async Task<ActionResult<List<CollectorPerformanceRowDto>>> GetTable(
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? collectorId,
        [FromQuery] int? agenceId, [FromQuery] int? departmentId, [FromQuery] int? zoneCollecteId, [FromQuery] string? status)
        => Ok(await _perfService.GetPerformanceTableAsync(BuildFilter(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status)));

    [HttpGet("{collectorId}")]
    public async Task<ActionResult<CollectorPerformanceDetailDto>> GetDetail(
        string collectorId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] int? agenceId, [FromQuery] int? departmentId, [FromQuery] int? zoneCollecteId, [FromQuery] string? status)
    {
        var filter = BuildFilter(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status);
        var detail = await _perfService.GetCollectorDetailAsync(collectorId, filter);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard(
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? collectorId,
        [FromQuery] int? agenceId, [FromQuery] int? departmentId, [FromQuery] int? zoneCollecteId,
        [FromQuery] string? status, [FromQuery] int top = 10)
        => Ok(await _perfService.GetLeaderboardAsync(BuildFilter(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status), top));

    [HttpGet("bottom-performers")]
    public async Task<ActionResult<List<BottomPerformerDto>>> GetBottomPerformers(
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? collectorId,
        [FromQuery] int? agenceId, [FromQuery] int? departmentId, [FromQuery] int? zoneCollecteId, [FromQuery] string? status)
        => Ok(await _perfService.GetBottomPerformersAsync(BuildFilter(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status)));

    [HttpGet("alerts")]
    public async Task<ActionResult<List<PerformanceAlertDto>>> GetAlerts()
        => Ok(await _perfService.GetAlertsAsync());

    [HttpGet("charts")]
    public async Task<ActionResult<List<ChartSeriesDto>>> GetCharts(
        [FromQuery] string type, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? collectorId, [FromQuery] int? agenceId, [FromQuery] int? departmentId,
        [FromQuery] int? zoneCollecteId, [FromQuery] string? status)
        => Ok(await _perfService.GetChartsAsync(type, BuildFilter(dateFrom, dateTo, collectorId, agenceId, departmentId, zoneCollecteId, status)));
}
