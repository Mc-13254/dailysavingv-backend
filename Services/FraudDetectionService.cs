using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

public record FraudFactor(string Rule, int Weight, string Description);

public interface IFraudDetectionService
{
    Task<FraudDetection> ScoreAndStoreAsync(Transactions tx);
}

/// <summary>
/// Computes a 0-100 fraud risk score from named, explainable business rules —
/// not a trained model. Each triggered rule contributes a fixed weight; the
/// total is capped at 100. Thresholds and weights are constants here rather
/// than admin-configurable yet (see limitations noted to the user).
/// </summary>
public class FraudDetectionService : IFraudDetectionService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    private const int HighRiskNotifyThreshold = 60;

    public FraudDetectionService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<FraudDetection> ScoreAndStoreAsync(Transactions tx)
    {
        var factors = new List<FraudFactor>();
        var now = tx.DateTransaction;

        // ---- Rule 1: Unusual amount vs this client's own history ----
        var clientHistory = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => t.ClientID == tx.ClientID && t.TransactionID != tx.TransactionID && t.TransactionType == tx.TransactionType)
            .OrderByDescending(t => t.DateTransaction).Take(50).ToListAsync();

        if (clientHistory.Count >= 3)
        {
            var avg = clientHistory.Average(t => t.Montant);
            if (avg > 0 && tx.Montant > avg * 4)
                factors.Add(new FraudFactor("UNUSUAL_AMOUNT_CLIENT", 25, $"Montant {tx.Montant:N0} très supérieur à la moyenne habituelle du client ({avg:N0})."));
        }

        // ---- Rule 2: Repeated transactions in a short window (same client) ----
        var recentClientTx = await _db.Transactions.IgnoreQueryFilters()
            .CountAsync(t => t.ClientID == tx.ClientID && t.TransactionID != tx.TransactionID && t.DateTransaction >= now.AddMinutes(-10));
        if (recentClientTx >= 3)
            factors.Add(new FraudFactor("REPEATED_CLIENT_TX", 20, $"{recentClientTx} autre(s) transaction(s) du même client dans les 10 dernières minutes."));

        // ---- Rule 3: Rapid multiple transactions by the same collector ----
        if (!string.IsNullOrWhiteSpace(tx.CollectorID))
        {
            var recentCollectorTx = await _db.Transactions.IgnoreQueryFilters()
                .CountAsync(t => t.CollectorID == tx.CollectorID && t.TransactionID != tx.TransactionID && t.DateTransaction >= now.AddMinutes(-5));
            if (recentCollectorTx >= 8)
                factors.Add(new FraudFactor("RAPID_COLLECTOR_TX", 15, $"{recentCollectorTx} transactions par ce collecteur dans les 5 dernières minutes."));
        }

        // ---- Rule 4: Outside business hours ----
        var calendar = await _db.BusinessCalendars.FirstOrDefaultAsync(c => c.AgenceID == tx.AgenceID);
        var workingDays = (calendar?.WorkingDays ?? "1,2,3,4,5,6").Split(',').Select(int.Parse).ToHashSet();
        var opening = calendar?.OpeningTime ?? new TimeSpan(8, 0, 0);
        var closing = calendar?.ClosingTime ?? new TimeSpan(17, 0, 0);
        var isoDay = (int)now.DayOfWeek == 0 ? 7 : (int)now.DayOfWeek;
        if (!workingDays.Contains(isoDay) || now.TimeOfDay < opening || now.TimeOfDay > closing)
            factors.Add(new FraudFactor("OUTSIDE_BUSINESS_HOURS", 15, "Transaction effectuée en dehors des heures ouvrables officielles de l'agence."));

        // ---- Rule 5: Dormant client suddenly reactivated with a large amount ----
        var lastPriorTx = clientHistory.FirstOrDefault();
        if (lastPriorTx != null && (now - lastPriorTx.DateTransaction).TotalDays > 90)
        {
            var avg = clientHistory.Average(t => t.Montant);
            if (avg > 0 && tx.Montant > avg * 2)
                factors.Add(new FraudFactor("DORMANT_REACTIVATION", 20, $"Client inactif depuis {(now - lastPriorTx.DateTransaction).TotalDays:N0} jours, réapparaît avec un montant supérieur à son historique."));
        }

        // ---- Rule 6: Agency-wide transaction spike this hour ----
        var agencyLastHour = await _db.Transactions.IgnoreQueryFilters()
            .CountAsync(t => t.AgenceID == tx.AgenceID && t.DateTransaction >= now.AddHours(-1));
        var agencyLastWeekSameHourAvg = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => t.AgenceID == tx.AgenceID && t.DateTransaction >= now.AddDays(-28) && t.DateTransaction < now.AddDays(-1))
            .CountAsync() / 28.0 / 24.0; // rough hourly baseline
        if (agencyLastWeekSameHourAvg > 0 && agencyLastHour > agencyLastWeekSameHourAvg * 3 && agencyLastHour >= 10)
            factors.Add(new FraudFactor("AGENCY_SPIKE", 10, $"Pic d'activité inhabituel dans l'agence : {agencyLastHour} transactions dans la dernière heure."));

        var score = Math.Min(100, factors.Sum(f => f.Weight));
        var riskLevel = score >= 85 ? "CRITICAL" : score >= 60 ? "HIGH" : score >= 30 ? "MEDIUM" : "LOW";
        var flagged = score >= HighRiskNotifyThreshold;

        var record = new FraudDetection
        {
            TransactionID = tx.TransactionID,
            Score = score,
            RiskLevel = riskLevel,
            FactorsJson = JsonSerializer.Serialize(factors),
            FlaggedForReview = flagged,
            ReviewStatus = flagged ? "PENDING" : "NONE"
        };
        _db.FraudDetections.Add(record);
        await _db.SaveChangesAsync();

        if (flagged)
        {
            await _notifications.SendToSupervisorsAsync(
                tx.AgenceID, $"Transaction à risque {riskLevel} détectée",
                $"Score de fraude {score}/100 sur la transaction {tx.ReceiptNumber} ({tx.Montant:N0}). {factors.Count} facteur(s) déclenché(s).",
                riskLevel == "CRITICAL" ? "ALERT" : "WARNING", "/security/fraud-detection"
            );
        }

        return record;
    }
}
