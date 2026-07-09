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

    // ---- Client Reports -----------------------------------------------

    [HttpGet("clients")]
    public async Task<ActionResult<IEnumerable<ClientReportRowDto>>> ClientReport(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int? agenceId,
        [FromQuery] string? collectorId, [FromQuery] bool? blacklisted)
    {
        var query = _db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Nom.Contains(search) || c.ClientID.Contains(search) || (c.PhoneNumber != null && c.PhoneNumber.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.ValidationStatus == status);
        if (agenceId.HasValue) query = query.Where(c => c.AgenceID == agenceId.Value);
        if (!string.IsNullOrWhiteSpace(collectorId)) query = query.Where(c => c.CollectorID == collectorId);
        if (blacklisted.HasValue) query = query.Where(c => c.IsBlacklisted == blacklisted.Value);

        var clients = await query.OrderByDescending(c => c.CreatedDate).Take(500).ToListAsync();
        var clientIds = clients.Select(c => c.ClientID).ToList();

        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => clients.Select(c => c.AgenceID).Contains(a.AgenceID)).ToListAsync();
        var collectorIds = clients.Where(c => c.CollectorID != null).Select(c => c.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();
        var accounts = await _db.Accounts.IgnoreQueryFilters().Where(a => clientIds.Contains(a.ClientID)).ToListAsync();
        var contracts = await _db.Contracts.IgnoreQueryFilters().Where(c => c.ClientID != null && clientIds.Contains(c.ClientID)).ToListAsync();
        var collections = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => clientIds.Contains(t.ClientID) && t.TransactionType == Entities.TransactionType.DAILY_COLLECTION)
            .ToListAsync();
        var lastTx = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => clientIds.Contains(t.ClientID))
            .GroupBy(t => t.ClientID)
            .Select(g => new { ClientID = g.Key, Last = g.Max(t => t.DateTransaction) })
            .ToListAsync();

        var result = clients.Select(c =>
        {
            var agence = agencies.FirstOrDefault(a => a.AgenceID == c.AgenceID);
            var collector = collectors.FirstOrDefault(co => co.CollectorID == c.CollectorID);
            var clientAccounts = accounts.Where(a => a.ClientID == c.ClientID).ToList();
            var clientCollections = collections.Where(t => t.ClientID == c.ClientID).ToList();
            var last = lastTx.FirstOrDefault(l => l.ClientID == c.ClientID);

            return new ClientReportRowDto(
                c.ClientID, $"{c.Nom} {c.Prenom}".Trim(), c.PhoneNumber, agence?.Nom ?? "—",
                collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
                clientAccounts.Count, clientAccounts.Sum(a => a.Balance),
                contracts.Count(ct => ct.ClientID == c.ClientID),
                clientCollections.Count, clientCollections.Sum(t => t.Montant), last?.Last,
                c.ValidationStatus, c.IsBlacklisted, c.CreatedDate
            );
        });

        return Ok(result);
    }

    [HttpGet("clients/{id}")]
    public async Task<ActionResult<ClientReportDetailDto>> ClientDetail(string id)
    {
        var c = await _db.Clients.FirstOrDefaultAsync(x => x.ClientID == id)
            ?? throw new KeyNotFoundException("Client introuvable.");

        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == c.AgenceID);
        var collector = c.CollectorID != null ? await _db.Collectors.IgnoreQueryFilters().FirstOrDefaultAsync(co => co.CollectorID == c.CollectorID) : null;
        var zone = c.ZoneCollecteID != null ? await _db.ZoneCollectes.FirstOrDefaultAsync(z => z.ZoneCollecteID == c.ZoneCollecteID.Value) : null;

        var accounts = await _db.Accounts.IgnoreQueryFilters().Where(a => a.ClientID == c.ClientID).ToListAsync();
        var contracts = await _db.Contracts.IgnoreQueryFilters().Where(ct => ct.ClientID == c.ClientID).ToListAsync();
        var contractTypeIds = contracts.Where(ct => ct.ContractTypeID.HasValue).Select(ct => ct.ContractTypeID!.Value).Distinct().ToList();
        var contractTypes = await _db.ContractTypes.Where(ctp => contractTypeIds.Contains(ctp.ContractTypeID)).ToListAsync();

        var tx = await _db.Transactions.IgnoreQueryFilters().Where(t => t.ClientID == c.ClientID).ToListAsync();
        decimal SumOf(Entities.TransactionType type) => tx.Where(t => t.TransactionType == type).Sum(t => t.Montant);

        return Ok(new ClientReportDetailDto(
            c.ClientID, $"{c.Nom} {c.Prenom}".Trim(), c.PhoneNumber, c.Email, c.Address,
            c.Sexe, c.DateOfBirth, c.Nationality, c.Occupation,
            agence?.Nom ?? "—", collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null, zone?.Libelle,
            c.ValidationStatus, c.IsBlacklisted, c.RiskLevel, c.CreatedDate,
            accounts.Select(a => new ClientAccountSummaryDto(a.AccountID, a.AccountType, a.Balance, a.Status)).ToList(),
            contracts.Select(ct => new ClientContractSummaryDto(
                ct.ContractID, ct.ContractNumber,
                contractTypes.FirstOrDefault(x => x.ContractTypeID == ct.ContractTypeID)?.ContractName,
                ct.Statut, ct.StartDate
            )).ToList(),
            SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
            SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
            tx.Count, tx.Count > 0 ? tx.Max(t => t.DateTransaction) : null
        ));
    }

    [HttpGet("clients/stats")]
    public async Task<ActionResult<ClientReportStatsDto>> ClientStats()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var clients = await _db.Clients.ToListAsync();
        var accountedClientIds = (await _db.Accounts.IgnoreQueryFilters().Select(a => a.ClientID).Distinct().ToListAsync()).ToHashSet();
        var contractedClientIds = (await _db.Contracts.IgnoreQueryFilters().Where(c => c.ClientID != null).Select(c => c.ClientID!).Distinct().ToListAsync()).ToHashSet();

        return Ok(new ClientReportStatsDto(
            clients.Count,
            clients.Count(c => c.ValidationStatus == "VALIDATED" && !c.IsBlacklisted),
            clients.Count(c => c.ValidationStatus == "PENDING"),
            clients.Count(c => c.IsBlacklisted),
            clients.Count(c => c.CreatedDate >= monthStart),
            clients.Count(c => accountedClientIds.Contains(c.ClientID)),
            clients.Count(c => contractedClientIds.Contains(c.ClientID))
        ));
    }

    // ---- Account Reports -----------------------------------------------

    [HttpGet("accounts")]
    public async Task<ActionResult<IEnumerable<AccountReportRowDto>>> AccountReport(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int? agenceId)
    {
        var query = _db.Accounts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.AccountID.Contains(search) || a.ClientID.Contains(search));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (agenceId.HasValue) query = query.Where(a => a.AgenceID == agenceId.Value);

        var accounts = await query.OrderByDescending(a => a.CreateDate).Take(500).ToListAsync();
        var clientIds = accounts.Select(a => a.ClientID).Distinct().ToList();
        var accountIds = accounts.Select(a => a.AccountID).ToList();

        var clients = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID)).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => accounts.Select(x => x.AgenceID).Contains(a.AgenceID)).ToListAsync();
        var collectorIds = accounts.Where(a => a.CollectorID != null).Select(a => a.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();
        var contractIds = accounts.Where(a => a.ContractID.HasValue).Select(a => a.ContractID!.Value).Distinct().ToList();
        var contracts = await _db.Contracts.IgnoreQueryFilters().Where(c => contractIds.Contains(c.ContractID)).ToListAsync();
        var lastTx = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => accountIds.Contains(t.AccountID))
            .GroupBy(t => t.AccountID)
            .Select(g => new { AccountID = g.Key, Last = g.Max(t => t.DateTransaction) })
            .ToListAsync();

        var result = accounts.Select(a =>
        {
            var client = clients.FirstOrDefault(c => c.ClientID == a.ClientID);
            var agence = agencies.FirstOrDefault(x => x.AgenceID == a.AgenceID);
            var collector = collectors.FirstOrDefault(c => c.CollectorID == a.CollectorID);
            var contract = contracts.FirstOrDefault(c => c.ContractID == a.ContractID);
            var last = lastTx.FirstOrDefault(l => l.AccountID == a.AccountID);

            return new AccountReportRowDto(
                a.AccountID, a.AccountType, a.ClientID, client != null ? $"{client.Nom} {client.Prenom}".Trim() : a.ClientID,
                agence?.Nom ?? "—", collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
                contract?.ContractNumber, a.Balance, a.AvailableBalance, a.Status, a.CreateDate, last?.Last
            );
        });

        return Ok(result);
    }

    [HttpGet("accounts/{id}")]
    public async Task<ActionResult<AccountReportDetailDto>> AccountDetail(string id)
    {
        var a = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountID == id)
            ?? throw new KeyNotFoundException("Compte introuvable.");

        var client = await _db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ClientID == a.ClientID);
        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.AgenceID == a.AgenceID);
        var collector = a.CollectorID != null ? await _db.Collectors.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CollectorID == a.CollectorID) : null;
        var contract = a.ContractID != null ? await _db.Contracts.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.ContractID == a.ContractID.Value) : null;
        var contractType = contract?.ContractTypeID != null ? await _db.ContractTypes.FirstOrDefaultAsync(ct => ct.ContractTypeID == contract.ContractTypeID.Value) : null;

        var tx = await _db.Transactions.IgnoreQueryFilters().Where(t => t.AccountID == a.AccountID).ToListAsync();
        decimal SumOf(Entities.TransactionType type) => tx.Where(t => t.TransactionType == type).Sum(t => t.Montant);

        return Ok(new AccountReportDetailDto(
            a.AccountID, a.AccountType, a.Currency, a.ClientID, client != null ? $"{client.Nom} {client.Prenom}".Trim() : a.ClientID,
            agence?.Nom ?? "—", collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
            contract?.ContractNumber, contractType?.ContractName,
            a.OpeningBalance, a.Balance, a.AvailableBalance, a.BlockedBalance,
            a.MinimumBalance, a.MaximumBalance,
            SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
            SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
            tx.Count, a.CreateDate, tx.Count > 0 ? tx.Max(t => t.DateTransaction) : null,
            a.Status, a.FreezeReason, a.CloseReason
        ));
    }

    [HttpGet("accounts/stats")]
    public async Task<ActionResult<AccountReportStatsDto>> AccountStats()
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var accounts = await _db.Accounts.ToListAsync();

        return Ok(new AccountReportStatsDto(
            accounts.Count,
            accounts.Count(a => a.Status == "ACTIVE"),
            accounts.Count(a => a.Status == "FROZEN"),
            accounts.Count(a => a.Status == "CLOSED"),
            accounts.Count(a => a.Status == "DORMANT"),
            accounts.Sum(a => a.Balance),
            accounts.Count > 0 ? accounts.Average(a => a.Balance) : 0,
            accounts.Count(a => a.CreateDate >= monthStart)
        ));
    }

    // ---- Contract Reports -----------------------------------------------

    [HttpGet("contracts")]
    public async Task<ActionResult<IEnumerable<ContractReportRowDto>>> ContractReport(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] int? agenceId)
    {
        var query = _db.Contracts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.ContractNumber.Contains(search) || (c.ClientID != null && c.ClientID.Contains(search)));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Statut == status);
        if (agenceId.HasValue) query = query.Where(c => c.AgenceID == agenceId.Value);

        var contracts = await query.OrderByDescending(c => c.StartDate).Take(500).ToListAsync();
        var clientIds = contracts.Where(c => c.ClientID != null).Select(c => c.ClientID!).Distinct().ToList();
        var contractIds = contracts.Select(c => c.ContractID).ToList();

        var clients = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID)).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => contracts.Select(c => c.AgenceID).Contains(a.AgenceID)).ToListAsync();
        var collectorIds = contracts.Where(c => c.CollectorID != null).Select(c => c.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();
        var contractTypeIds = contracts.Where(c => c.ContractTypeID.HasValue).Select(c => c.ContractTypeID!.Value).Distinct().ToList();
        var contractTypes = await _db.ContractTypes.Where(ct => contractTypeIds.Contains(ct.ContractTypeID)).ToListAsync();
        var commissionTypeIds = contracts.Where(c => c.CommissionTypeID.HasValue).Select(c => c.CommissionTypeID!.Value).Distinct().ToList();
        var commissionTypes = await _db.CommissionTypes.Where(ct => commissionTypeIds.Contains(ct.CommissionTypeID)).ToListAsync();
        var accounts = await _db.Accounts.IgnoreQueryFilters().Where(a => a.ContractID.HasValue && contractIds.Contains(a.ContractID.Value)).ToListAsync();
        var commissions = await _db.Transactions.IgnoreQueryFilters()
            .Where(t => t.AccountID != null)
            .ToListAsync();

        var result = contracts.Select(c =>
        {
            var client = clients.FirstOrDefault(x => x.ClientID == c.ClientID);
            var agence = agencies.FirstOrDefault(a => a.AgenceID == c.AgenceID);
            var collector = collectors.FirstOrDefault(x => x.CollectorID == c.CollectorID);
            var contractType = contractTypes.FirstOrDefault(ct => ct.ContractTypeID == c.ContractTypeID);
            var commissionType = commissionTypes.FirstOrDefault(ct => ct.CommissionTypeID == c.CommissionTypeID);
            var account = accounts.FirstOrDefault(a => a.ContractID == c.ContractID);
            var commissionTotal = account != null ? commissions.Where(t => t.AccountID == account.AccountID).Sum(t => t.MontantCommission) : 0;

            return new ContractReportRowDto(
                c.ContractID, c.ContractNumber, c.ClientID ?? "—", client != null ? $"{client.Nom} {client.Prenom}".Trim() : c.ClientID ?? "—",
                agence?.Nom ?? "—", collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
                contractType?.ContractName, commissionType?.Name,
                account?.Balance, commissionTotal, c.Statut, c.StartDate, c.EndDate
            );
        });

        return Ok(result);
    }

    [HttpGet("contracts/{id:int}")]
    public async Task<ActionResult<ContractReportDetailDto>> ContractDetail(int id)
    {
        var c = await _db.Contracts.FirstOrDefaultAsync(x => x.ContractID == id)
            ?? throw new KeyNotFoundException("Contrat introuvable.");

        var client = c.ClientID != null ? await _db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.ClientID == c.ClientID) : null;
        var agence = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.AgenceID == c.AgenceID);
        var collector = c.CollectorID != null ? await _db.Collectors.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.CollectorID == c.CollectorID) : null;
        var contractType = c.ContractTypeID != null ? await _db.ContractTypes.FirstOrDefaultAsync(ct => ct.ContractTypeID == c.ContractTypeID.Value) : null;
        var commissionType = c.CommissionTypeID != null ? await _db.CommissionTypes.FirstOrDefaultAsync(ct => ct.CommissionTypeID == c.CommissionTypeID.Value) : null;
        var account = await _db.Accounts.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.ContractID == c.ContractID);

        var tx = account != null
            ? await _db.Transactions.IgnoreQueryFilters().Where(t => t.AccountID == account.AccountID && t.TransactionType == Entities.TransactionType.DAILY_COLLECTION).ToListAsync()
            : new List<Entities.Transactions>();
        var totalCollected = tx.Sum(t => t.Montant);
        var commissionGenerated = tx.Sum(t => t.MontantCommission);

        return Ok(new ContractReportDetailDto(
            c.ContractID, c.ContractNumber, c.ClientID ?? "—", client != null ? $"{client.Nom} {client.Prenom}".Trim() : c.ClientID ?? "—",
            agence?.Nom ?? "—", collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
            contractType?.ContractName, commissionType?.Name,
            c.StartDate, c.EndDate, c.Statut, c.TerminationReason, c.TerminationDate,
            account?.AccountID, account?.Balance,
            totalCollected, tx.Count, tx.Count > 0 ? totalCollected / tx.Count : 0,
            commissionGenerated, totalCollected > 0 ? commissionGenerated / totalCollected * 100 : null
        ));
    }

    [HttpGet("contracts/stats")]
    public async Task<ActionResult<ContractReportStatsDto>> ContractStats()
    {
        var in30Days = DateTime.UtcNow.AddDays(30);
        var contracts = await _db.Contracts.ToListAsync();
        var accounts = await _db.Accounts.IgnoreQueryFilters().ToListAsync();
        var accountIds = accounts.Select(a => a.AccountID).ToHashSet();
        var commissions = await _db.Transactions.IgnoreQueryFilters().Where(t => accountIds.Contains(t.AccountID)).SumAsync(t => t.MontantCommission);

        return Ok(new ContractReportStatsDto(
            contracts.Count,
            contracts.Count(c => c.Statut == "ACTIVE"),
            contracts.Count(c => c.Statut == "TERMINATED"),
            contracts.Count(c => c.Statut == "ACTIVE" && c.EndDate.HasValue && c.EndDate.Value <= in30Days),
            commissions
        ));
    }

    // ---- Commission Reports -----------------------------------------------

    [HttpGet("commissions")]
    public async Task<ActionResult<IEnumerable<CommissionReportRowDto>>> CommissionReport(
        [FromQuery] string? collectorId, [FromQuery] int? agenceId, [FromQuery] int? commissionTypeId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.Transactions.Where(t => t.MontantCommission > 0);
        if (!string.IsNullOrWhiteSpace(collectorId)) query = query.Where(t => t.CollectorID == collectorId);
        if (agenceId.HasValue) query = query.Where(t => t.AgenceID == agenceId.Value);
        if (commissionTypeId.HasValue) query = query.Where(t => t.CommissionTypeID == commissionTypeId.Value);
        if (from.HasValue) query = query.Where(t => t.DateTransaction >= from.Value);
        if (to.HasValue) query = query.Where(t => t.DateTransaction <= to.Value.AddDays(1).AddTicks(-1));

        var tx = await query.OrderByDescending(t => t.DateTransaction).Take(500).ToListAsync();

        var clientIds = tx.Select(t => t.ClientID).Distinct().ToList();
        var clients = await _db.Clients.IgnoreQueryFilters().Where(c => clientIds.Contains(c.ClientID)).ToListAsync();
        var collectorIds = tx.Where(t => t.CollectorID != null).Select(t => t.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => tx.Select(t => t.AgenceID).Contains(a.AgenceID)).ToListAsync();
        var commissionTypeIds = tx.Where(t => t.CommissionTypeID.HasValue).Select(t => t.CommissionTypeID!.Value).Distinct().ToList();
        var commissionTypes = await _db.CommissionTypes.Where(ct => commissionTypeIds.Contains(ct.CommissionTypeID)).ToListAsync();
        var accountIds = tx.Select(t => t.AccountID).Distinct().ToList();
        var accounts = await _db.Accounts.IgnoreQueryFilters().Where(a => accountIds.Contains(a.AccountID)).ToListAsync();

        var result = tx.Select(t =>
        {
            var client = clients.FirstOrDefault(c => c.ClientID == t.ClientID);
            var collector = collectors.FirstOrDefault(c => c.CollectorID == t.CollectorID);
            var agence = agencies.FirstOrDefault(a => a.AgenceID == t.AgenceID);
            var commType = commissionTypes.FirstOrDefault(ct => ct.CommissionTypeID == t.CommissionTypeID);
            var account = accounts.FirstOrDefault(a => a.AccountID == t.AccountID);

            return new CommissionReportRowDto(
                t.TransactionID, t.ReceiptNumber, t.CollectorID, collector != null ? $"{collector.Name} {collector.Surname}".Trim() : null,
                agence?.Nom ?? "—", t.ClientID, client != null ? $"{client.Nom} {client.Prenom}".Trim() : t.ClientID,
                account?.ContractID?.ToString(), commType?.Name, t.Montant, t.MontantCommission, t.DateTransaction
            );
        });

        return Ok(result);
    }

    [HttpGet("commissions/by-collector")]
    public async Task<ActionResult<IEnumerable<CommissionByGroupDto>>> CommissionByCollector()
    {
        var tx = await _db.Transactions.Where(t => t.MontantCommission > 0 && t.CollectorID != null).ToListAsync();
        var collectorIds = tx.Select(t => t.CollectorID!).Distinct().ToList();
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => collectorIds.Contains(c.CollectorID)).ToListAsync();

        var grouped = tx.GroupBy(t => t.CollectorID).Select(g =>
        {
            var collector = collectors.FirstOrDefault(c => c.CollectorID == g.Key);
            return new CommissionByGroupDto(collector != null ? $"{collector.Name} {collector.Surname}".Trim() : g.Key!, g.Sum(t => t.MontantCommission), g.Sum(t => t.Montant), g.Count());
        }).OrderByDescending(x => x.CommissionAmount).Take(10);

        return Ok(grouped);
    }

    [HttpGet("commissions/by-agency")]
    public async Task<ActionResult<IEnumerable<CommissionByGroupDto>>> CommissionByAgency()
    {
        var tx = await _db.Transactions.Where(t => t.MontantCommission > 0).ToListAsync();
        var agencies = await _db.Agences.IgnoreQueryFilters().Where(a => tx.Select(t => t.AgenceID).Contains(a.AgenceID)).ToListAsync();

        var grouped = tx.GroupBy(t => t.AgenceID).Select(g =>
        {
            var agence = agencies.FirstOrDefault(a => a.AgenceID == g.Key);
            return new CommissionByGroupDto(agence?.Nom ?? $"Agence {g.Key}", g.Sum(t => t.MontantCommission), g.Sum(t => t.Montant), g.Count());
        }).OrderByDescending(x => x.CommissionAmount);

        return Ok(grouped);
    }

    [HttpGet("commissions/stats")]
    public async Task<ActionResult<CommissionReportStatsDto>> CommissionStats()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var yearStart = new DateTime(today.Year, 1, 1);
        var tx = await _db.Transactions.Where(t => t.MontantCommission > 0).ToListAsync();

        return Ok(new CommissionReportStatsDto(
            tx.Sum(t => t.MontantCommission),
            tx.Where(t => t.DateTransaction >= today).Sum(t => t.MontantCommission),
            tx.Where(t => t.DateTransaction >= monthStart).Sum(t => t.MontantCommission),
            tx.Where(t => t.DateTransaction >= yearStart).Sum(t => t.MontantCommission),
            tx.Count > 0 ? tx.Average(t => t.MontantCommission) : 0,
            tx.Count > 0 ? tx.Max(t => t.MontantCommission) : 0,
            tx.Count > 0 ? tx.Min(t => t.MontantCommission) : 0
        ));
    }

    // ---- Agency Reports -----------------------------------------------

    [HttpGet("agencies")]
    public async Task<ActionResult<IEnumerable<AgencyReportRowDto>>> AgencyReport()
    {
        var agencies = await _db.Agences.IgnoreQueryFilters().ToListAsync();
        var managers = await _db.Users.IgnoreQueryFilters().Where(u => agencies.Select(a => a.ManagerId).Contains(u.CodeUser)).ToListAsync();
        var collectors = await _db.Collectors.IgnoreQueryFilters().ToListAsync();
        var clients = await _db.Clients.IgnoreQueryFilters().ToListAsync();
        var accounts = await _db.Accounts.IgnoreQueryFilters().ToListAsync();
        var tx = await _db.Transactions.IgnoreQueryFilters().ToListAsync();
        var sessions = await _db.CashSessions.ToListAsync();

        var rows = agencies.Select(a =>
        {
            var manager = managers.FirstOrDefault(u => u.CodeUser == a.ManagerId);
            var agencyTx = tx.Where(t => t.AgenceID == a.AgenceID).ToList();
            decimal SumOf(Entities.TransactionType type) => agencyTx.Where(t => t.TransactionType == type).Sum(t => t.Montant);
            var agencySessions = sessions.Where(s => s.AgenceID == a.AgenceID && s.CashDifference.HasValue).ToList();

            return new AgencyReportRowDto(
                a.AgenceID, a.CodeAgence, a.Nom, manager != null ? $"{manager.FirstName} {manager.LastName}".Trim() : null,
                collectors.Count(c => c.AgenceID == a.AgenceID), clients.Count(c => c.AgenceID == a.AgenceID),
                accounts.Count(acc => acc.AgenceID == a.AgenceID),
                SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
                SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
                agencyTx.Sum(t => t.MontantCommission), agencySessions.Sum(s => Math.Abs(s.CashDifference ?? 0)), 0
            );
        }).OrderByDescending(a => a.Collections + a.Deposits).ToList();

        var ranked = rows.Select((r, i) => r with { Rank = i + 1 });
        return Ok(ranked);
    }

    [HttpGet("agencies/{id:int}")]
    public async Task<ActionResult<AgencyReportDetailDto>> AgencyDetail(int id)
    {
        var a = await _db.Agences.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.AgenceID == id)
            ?? throw new KeyNotFoundException("Agence introuvable.");

        var manager = a.ManagerId != null ? await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.CodeUser == a.ManagerId) : null;
        var collectors = await _db.Collectors.IgnoreQueryFilters().Where(c => c.AgenceID == a.AgenceID).ToListAsync();
        var cashiers = await _db.Users.IgnoreQueryFilters().Where(u => u.AgenceID == a.AgenceID).ToListAsync();
        var clients = await _db.Clients.IgnoreQueryFilters().Where(c => c.AgenceID == a.AgenceID).ToListAsync();
        var accounts = await _db.Accounts.IgnoreQueryFilters().Where(acc => acc.AgenceID == a.AgenceID).ToListAsync();
        var contracts = await _db.Contracts.IgnoreQueryFilters().Where(c => c.AgenceID == a.AgenceID).ToListAsync();
        var tx = await _db.Transactions.IgnoreQueryFilters().Where(t => t.AgenceID == a.AgenceID).ToListAsync();
        var sessions = await _db.CashSessions.Where(s => s.AgenceID == a.AgenceID).ToListAsync();

        decimal SumOf(Entities.TransactionType type) => tx.Where(t => t.TransactionType == type).Sum(t => t.Montant);

        var allAgencies = await _db.Agences.IgnoreQueryFilters().CountAsync();
        var allTx = await _db.Transactions.IgnoreQueryFilters().ToListAsync();
        var ranking = allTx.GroupBy(t => t.AgenceID)
            .Select(g => new { AgenceID = g.Key, Total = g.Sum(t => t.Montant) })
            .OrderByDescending(x => x.Total).ToList();
        var rank = ranking.FindIndex(x => x.AgenceID == a.AgenceID) + 1;

        return Ok(new AgencyReportDetailDto(
            a.AgenceID, a.CodeAgence, a.Nom, a.Address, a.PrimaryPhone, a.Email,
            manager != null ? $"{manager.FirstName} {manager.LastName}".Trim() : null,
            collectors.Count, cashiers.Count, clients.Count, accounts.Count, contracts.Count,
            SumOf(Entities.TransactionType.DAILY_COLLECTION), SumOf(Entities.TransactionType.DEPOSIT),
            SumOf(Entities.TransactionType.WITHDRAWAL), SumOf(Entities.TransactionType.TRANSFER),
            tx.Sum(t => t.MontantCommission), accounts.Sum(acc => acc.Balance),
            sessions.Count(s => s.Status == "OPEN"), sessions.Count(s => s.Status == "CLOSED"),
            sessions.Where(s => s.CashDifference.HasValue).Sum(s => Math.Abs(s.CashDifference ?? 0)),
            rank <= 0 ? allAgencies : rank, allAgencies
        ));
    }

    [HttpGet("agencies/stats")]
    public async Task<ActionResult<AgencyReportStatsDto>> AgencyStats()
    {
        var agencies = await _db.Agences.IgnoreQueryFilters().ToListAsync();
        var tx = await _db.Transactions.IgnoreQueryFilters().ToListAsync();
        var clients = await _db.Clients.IgnoreQueryFilters().CountAsync();
        var collectors = await _db.Collectors.IgnoreQueryFilters().CountAsync();

        var byAgency = tx.GroupBy(t => t.AgenceID)
            .Select(g => new { AgenceID = g.Key, Total = g.Sum(t => t.Montant) })
            .OrderByDescending(x => x.Total).ToList();

        var top = byAgency.FirstOrDefault();
        var bottom = byAgency.LastOrDefault();

        return Ok(new AgencyReportStatsDto(
            agencies.Count,
            top != null ? agencies.FirstOrDefault(a => a.AgenceID == top.AgenceID)?.Nom : null,
            bottom != null ? agencies.FirstOrDefault(a => a.AgenceID == bottom.AgenceID)?.Nom : null,
            tx.Sum(t => t.Montant), tx.Sum(t => t.MontantCommission), clients, collectors
        ));
    }
}
