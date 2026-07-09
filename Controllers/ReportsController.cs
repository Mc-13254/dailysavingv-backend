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
            await _db.ClientTmps.CountAsync(x => x.PendingStatus == Entities.Pending.PendingStatus.PENDING) +
            await _db.CollectorTMPs.CountAsync(x => x.PendingStatus == Entities.Pending.PendingStatus.PENDING) +
            await _db.ContractTmps.CountAsync(x => x.PendingStatus == Entities.Pending.PendingStatus.PENDING) +
            await _db.AccountsTMPs.CountAsync(x => x.PendingStatus == Entities.Pending.PendingStatus.PENDING) +
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

    // ---- Cash Session Reports ---------------------------------------------

    [HttpGet("cash-sessions")]
    public async Task<ActionResult<IEnumerable<CashSessionReportRowDto>>> CashSessionReport(
        [FromQuery] string? codeUser, [FromQuery] int? agenceId, [FromQuery] string? status,
        [FromQuery] bool? balanced, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.CashSessions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(codeUser)) query = query.Where(s => s.CodeUser == codeUser);
        if (agenceId.HasValue) query = query.Where(s => s.AgenceID == agenceId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (from.HasValue) query = query.Where(s => s.OpeningDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.OpeningDate <= to.Value.AddDays(1).AddTicks(-1));
        if (balanced.HasValue)
            query = balanced.Value ? query.Where(s => s.CashDifference == 0 || s.CashDifference == null) : query.Where(s => s.CashDifference != null && s.CashDifference != 0);

        var sessions = await query.OrderByDescending(s => s.OpeningDate).Take(300).ToListAsync();

        var users = await _db.Users.IgnoreQueryFilters().Where(u => sessions.Select(s => s.CodeUser).Contains(u.CodeUser)).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => sessions.Select(s => s.AgenceID).Contains(a.AgenceID)).ToListAsync();
        var sessionIds = sessions.Select(s => s.CashSessionID).ToList();
        var transactions = await _db.Transactions.IgnoreQueryFilters().Where(t => t.CashSessionID != null && sessionIds.Contains(t.CashSessionID.Value)).ToListAsync();

        var result = sessions.Select(s =>
        {
            var user = users.FirstOrDefault(u => u.CodeUser == s.CodeUser);
            var agence = agencies.FirstOrDefault(a => a.AgenceID == s.AgenceID);
            var tx = transactions.Where(t => t.CashSessionID == s.CashSessionID).ToList();
            decimal SumOf(Entities.TransactionType type) => tx.Where(t => t.TransactionType == type).Sum(t => t.Montant);

            return new CashSessionReportRowDto(
                s.CashSessionID, s.SessionNumber, s.CodeUser, user != null ? $"{user.FirstName} {user.LastName}".Trim() : s.CodeUser,
                agence?.Nom ?? "—", s.OpeningDate, s.ClosingDate, s.OpeningCash,
                s.ExpectedCash, s.PhysicalCash, s.CashDifference,
                SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
                SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
                s.Status, s.RequiresApproval, s.ApprovalStatus
            );
        });

        return Ok(result);
    }

    [HttpGet("cash-sessions/{id:int}")]
    public async Task<ActionResult<CashSessionReportDetailDto>> CashSessionDetail(int id)
    {
        var s = await _db.CashSessions.FirstOrDefaultAsync(x => x.CashSessionID == id)
            ?? throw new KeyNotFoundException("Session de caisse introuvable.");

        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == s.CodeUser);
        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == s.AgenceID);
        var tx = await _db.Transactions.IgnoreQueryFilters().Where(t => t.CashSessionID == s.CashSessionID).ToListAsync();
        var variance = await _db.CashVariances.FirstOrDefaultAsync(v => v.CashSessionID == s.CashSessionID);

        decimal SumOf(Entities.TransactionType type) => tx.Where(t => t.TransactionType == type).Sum(t => t.Montant);

        return Ok(new CashSessionReportDetailDto(
            s.CashSessionID, s.SessionNumber, s.CodeUser, user != null ? $"{user.FirstName} {user.LastName}".Trim() : s.CodeUser,
            s.AgenceID, agence?.Nom ?? "—",
            s.OpeningDate, s.OpeningCash, s.PreviousClosingCash, s.OpeningComment,
            s.ClosingDate, s.ExpectedCash, s.PhysicalCash, s.PhysicalCashBreakdownJson,
            s.CashDifference, s.ClosingComment, s.ClosedBy,
            SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
            SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
            tx.Sum(t => t.MontantCommission), tx.Count,
            s.Status, s.RequiresApproval, s.ApprovalStatus, s.ApprovedBy, s.ApprovalDate,
            variance?.Reason, variance?.Comment
        ));
    }

    [HttpGet("cash-sessions/stats")]
    public async Task<ActionResult<CashSessionReportStatsDto>> CashSessionStats()
    {
        var today = DateTime.UtcNow.Date;
        var all = await _db.CashSessions.ToListAsync();
        var todayCount = all.Count(s => s.OpeningDate >= today);
        var closed = all.Where(s => s.Status == "CLOSED").ToList();
        var balanced = closed.Count(s => s.CashDifference == 0);
        var unbalanced = closed.Count(s => s.CashDifference != null && s.CashDifference != 0);
        var durations = closed.Where(s => s.ClosingDate.HasValue).Select(s => (s.ClosingDate!.Value - s.OpeningDate).TotalHours).ToList();

        return Ok(new CashSessionReportStatsDto(
            todayCount, all.Count(s => s.Status == "OPEN"), closed.Count,
            balanced, unbalanced,
            closed.Sum(s => s.ExpectedCash ?? 0), closed.Sum(s => s.PhysicalCash ?? 0), closed.Sum(s => s.CashDifference ?? 0),
            durations.Count > 0 ? durations.Average() : 0
        ));
    }
}
