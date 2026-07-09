using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

/// <summary>
/// Central Reports module. Transaction History is the foundational report —
/// the official read-only ledger every other report (Collector, Client,
/// Financial...) will eventually reconcile against. Report Center is the hub
/// that links out to every report with a quick live count.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    // ---- Transaction History (read-only ledger) --------------------------

    [HttpGet("transaction-history")]
    public async Task<ActionResult<IEnumerable<TransactionHistoryRowDto>>> TransactionHistory(
        [FromQuery] string? search, [FromQuery] string? transactionType, [FromQuery] string? status,
        [FromQuery] string? paymentMethod, [FromQuery] int? agenceId, [FromQuery] string? collectorId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.Transactions.AsQueryable();

        if (from.HasValue) query = query.Where(t => t.DateTransaction >= from.Value);
        if (to.HasValue) query = query.Where(t => t.DateTransaction <= to.Value.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(transactionType) && Enum.TryParse<Entities.TransactionType>(transactionType, out var tt))
            query = query.Where(t => t.TransactionType == tt);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Statut == status);
        if (!string.IsNullOrWhiteSpace(paymentMethod)) query = query.Where(t => t.PaymentMethod == paymentMethod);
        if (agenceId.HasValue) query = query.Where(t => t.AgenceID == agenceId.Value);
        if (!string.IsNullOrWhiteSpace(collectorId)) query = query.Where(t => t.CollectorID == collectorId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                (t.ReceiptNumber != null && t.ReceiptNumber.Contains(search)) ||
                t.ClientID.Contains(search) || t.AccountID.Contains(search) ||
                (t.RemitterName != null && t.RemitterName.Contains(search)) ||
                (t.BeneficiaryName != null && t.BeneficiaryName.Contains(search)));

        var transactions = await query.OrderByDescending(t => t.DateTransaction).Take(500).ToListAsync();

        var clientIds = transactions.Select(t => t.ClientID).Distinct().ToList();
        var clients = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID)).ToListAsync();
        var collectorIds = transactions.Where(t => t.CollectorID != null).Select(t => t.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();
        var agencyIds = transactions.Select(t => t.AgenceID).Distinct().ToList();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => agencyIds.Contains(a.AgenceID)).ToListAsync();

        var result = transactions.Select(t =>
        {
            var client = clients.FirstOrDefault(c => c.ClientID == t.ClientID);
            var collector = collectors.FirstOrDefault(c => c.CollectorID == t.CollectorID);
            var agence = agencies.FirstOrDefault(a => a.AgenceID == t.AgenceID);
            return new TransactionHistoryRowDto(
                t.TransactionID, t.ReceiptNumber, t.TransactionType.ToString(),
                t.AccountID, t.ToAccountID, t.ClientID,
                client != null ? $"{client.Nom} {client.Prenom}".Trim() : t.ClientID,
                t.CollectorID, collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
                agence?.Nom ?? "—",
                t.Montant, t.MontantCommission, t.PaymentMethod, t.Statut, t.DateTransaction
            );
        });

        return Ok(result);
    }

    [HttpGet("transaction-history/{id:long}")]
    public async Task<ActionResult<TransactionHistoryDetailDto>> TransactionDetail(long id)
    {
        var t = await _db.Transactions.FirstOrDefaultAsync(x => x.TransactionID == id)
            ?? throw new KeyNotFoundException("Transaction introuvable.");

        var client = await _db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ClientID == t.ClientID);
        var toClient = t.ToClientID != null ? await _db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ClientID == t.ToClientID) : null;
        var account = await _db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AccountID == t.AccountID);
        var contract = account?.ContractID != null
            ? await _db.Contracts.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ContractID == account.ContractID.Value)
            : null;
        var collector = t.CollectorID != null
            ? await _db.Collectors.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CollectorID == t.CollectorID)
            : null;
        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == t.AgenceID);
        var session = t.CashSessionID != null
            ? await _db.CashSessions.FirstOrDefaultAsync(s => s.CashSessionID == t.CashSessionID.Value)
            : null;

        return Ok(new TransactionHistoryDetailDto(
            t.TransactionID, t.ReceiptNumber, t.TransactionType.ToString(),
            t.AccountID, account?.AccountType, t.ToAccountID,
            t.ClientID, client != null ? $"{client.Nom} {client.Prenom}".Trim() : t.ClientID,
            t.ToClientID, toClient != null ? $"{toClient.Nom} {toClient.Prenom}".Trim() : null,
            t.CollectorID, collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
            contract?.ContractNumber, t.AgenceID, agence?.Nom ?? "—",
            t.CashSessionID, session?.SessionNumber,
            t.Montant, t.OpeningBalance, t.ClosingBalance, t.MontantCommission,
            t.RemitterName, t.BeneficiaryName, t.PaymentMethod,
            t.ReferenceNumber, t.Comment, t.CashBreakdownJson,
            t.Statut, t.DateTransaction,
            t.CreatedBy, t.ValidatedBy, t.ValidationDate
        ));
    }

    [HttpGet("transaction-history/stats")]
    public async Task<ActionResult<TransactionHistoryStatsDto>> TransactionStats()
    {
        var today = DateTime.UtcNow.Date;
        var all = await _db.Transactions.ToListAsync();
        var todayTx = all.Where(t => t.DateTransaction >= today).ToList();

        decimal SumOf(Entities.TransactionType type) => all.Where(t => t.TransactionType == type).Sum(t => t.Montant);

        return Ok(new TransactionHistoryStatsDto(
            todayTx.Count, todayTx.Sum(t => t.Montant),
            SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
            SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
            all.Count(t => t.Statut == "PENDING"), all.Count(t => t.Statut == "VALIDATED"), all.Count(t => t.Statut is "REJECTED" or "CANCELLED")
        ));
    }

    // ---- Report Center hub -------------------------------------------------

    [HttpGet("center")]
    public async Task<ActionResult<IEnumerable<ReportCenterCardDto>>> Center()
    {
        var today = DateTime.UtcNow.Date;
        var transactions = await _db.Transactions.Where(t => t.DateTransaction >= today).ToListAsync();
        var pendingImports = await _db.TransactionImportRows.CountAsync(r => r.Status == "PENDING");
        var pendingValidations =
            await _db.ClientTmps.CountAsync(x => x.PendingStatus == "PENDING") +
            await _db.CollectorTMPs.CountAsync(x => x.PendingStatus == "PENDING") +
            await _db.ContractTmps.CountAsync(x => x.PendingStatus == "PENDING") +
            await _db.AccountsTMPs.CountAsync(x => x.PendingStatus == "PENDING") +
            pendingImports;
        var openSessions = await _db.CashSessions.CountAsync(s => s.Status == "OPEN");
        var totalClients = await _db.Clients.CountAsync(c => c.ValidationStatus == "VALIDATED");
        var totalCollectors = await _db.Collectors.CountAsync(c => c.IsActive);

        return Ok(new[]
        {
            new ReportCenterCardDto("transactions_today", "Transactions aujourd'hui", transactions.Count, transactions.Sum(t => t.Montant)),
            new ReportCenterCardDto("pending_validations", "En attente de validation", pendingValidations, null),
            new ReportCenterCardDto("open_sessions", "Sessions de caisse ouvertes", openSessions, null),
            new ReportCenterCardDto("active_clients", "Clients actifs", totalClients, null),
            new ReportCenterCardDto("active_collectors", "Collecteurs actifs", totalCollectors, null),
        });
    }
}
