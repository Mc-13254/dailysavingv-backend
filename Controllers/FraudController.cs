using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Services;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

[ApiController]
[Route("api/fraud")]
[Authorize(Policy = "SupervisorOrAdmin")]
public class FraudController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FraudController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<FraudRowDto>>> Transactions(
        [FromQuery] string? riskLevel, [FromQuery] string? reviewStatus, [FromQuery] bool? flaggedOnly)
    {
        var query = _db.FraudDetections.AsQueryable();
        if (!string.IsNullOrWhiteSpace(riskLevel)) query = query.Where(f => f.RiskLevel == riskLevel);
        if (!string.IsNullOrWhiteSpace(reviewStatus)) query = query.Where(f => f.ReviewStatus == reviewStatus);
        if (flaggedOnly == true) query = query.Where(f => f.FlaggedForReview);

        var frauds = await query.OrderByDescending(f => f.CreatedDate).Take(300).ToListAsync();
        var txIds = frauds.Select(f => f.TransactionID).ToList();
        var tx = await _db.Transactions.IgnoreQueryFilters().Where(t => txIds.Contains(t.TransactionID)).ToListAsync();

        var clientIds = tx.Select(t => t.ClientID).Distinct().ToList();
        var clients = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID)).ToListAsync();
        var collectorIds = tx.Where(t => t.CollectorID != null).Select(t => t.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => tx.Select(t => t.AgenceID).Contains(a.AgenceID)).ToListAsync();

        var result = frauds.Select(f =>
        {
            var t = tx.FirstOrDefault(t => t.TransactionID == f.TransactionID);
            var client = t != null ? clients.FirstOrDefault(c => c.ClientID == t.ClientID) : null;
            var collector = t?.CollectorID != null ? collectors.FirstOrDefault(c => c.CollectorID == t.CollectorID) : null;
            var agence = t != null ? agencies.FirstOrDefault(a => a.AgenceID == t.AgenceID) : null;

            return new FraudRowDto(
                f.FraudDetectionID, f.TransactionID, t?.ReceiptNumber, t?.TransactionType.ToString() ?? "—",
                client != null ? $"{client.Nom} {client.Prenom}".Trim() : t?.ClientID ?? "—",
                collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
                agence?.Nom ?? "—", t?.Montant ?? 0,
                f.Score, f.RiskLevel, f.FlaggedForReview, f.ReviewStatus, f.CreatedDate
            );
        });

        return Ok(result);
    }

    [HttpGet("transactions/{id:int}")]
    public async Task<ActionResult<FraudDetailDto>> Detail(int id)
    {
        var f = await _db.FraudDetections.FirstOrDefaultAsync(x => x.FraudDetectionID == id)
            ?? throw new KeyNotFoundException("Enregistrement introuvable.");
        var tx = await _db.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TransactionID == f.TransactionID);
        var client = tx != null ? await _db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ClientID == tx.ClientID) : null;
        var collector = tx?.CollectorID != null ? await _db.Collectors.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CollectorID == tx.CollectorID) : null;
        var agence = tx != null ? await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == tx.AgenceID) : null;

        var factors = JsonSerializer.Deserialize<List<FraudFactorDto>>(f.FactorsJson) ?? new();

        return Ok(new FraudDetailDto(
            f.FraudDetectionID, f.TransactionID, tx?.ReceiptNumber,
            client != null ? $"{client.Nom} {client.Prenom}".Trim() : tx?.ClientID ?? "—",
            collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
            agence?.Nom ?? "—", tx?.Montant ?? 0, tx?.DateTransaction ?? f.CreatedDate,
            f.Score, f.RiskLevel, factors,
            f.FlaggedForReview, f.ReviewStatus, f.ReviewedBy, f.ReviewDate, f.ReviewComment
        ));
    }

    [HttpPost("transactions/{id:int}/review")]
    public async Task<ActionResult> Review(int id, ReviewFraudRequest request)
    {
        if (request.ReviewStatus is not ("CLEARED" or "CONFIRMED_FRAUD"))
            throw new InvalidOperationException("Statut de revue invalide.");

        var f = await _db.FraudDetections.FirstOrDefaultAsync(x => x.FraudDetectionID == id)
            ?? throw new KeyNotFoundException("Enregistrement introuvable.");

        f.ReviewStatus = request.ReviewStatus;
        f.ReviewComment = request.Comment;
        f.ReviewedBy = _currentUser.CodeUser;
        f.ReviewDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Revue enregistrée." });
    }

    [HttpGet("stats")]
    public async Task<ActionResult<FraudStatsDto>> Stats()
    {
        var today = DateTime.UtcNow.Date;
        var all = await _db.FraudDetections.ToListAsync();

        return Ok(new FraudStatsDto(
            all.Count(f => f.CreatedDate >= today && f.FlaggedForReview),
            all.Count(f => f.ReviewStatus == "PENDING"),
            all.Count(f => f.RiskLevel == "CRITICAL"),
            all.Count(f => f.RiskLevel == "HIGH"),
            all.Count > 0 ? all.Average(f => f.Score) : 0,
            all.Count(f => f.ReviewStatus == "CONFIRMED_FRAUD"),
            all.Count(f => f.ReviewStatus == "CLEARED")
        ));
    }
}
