using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services;
using DailySavingV.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Controllers;

/// <summary>
/// Read-only financial statements computed from JournalEntry/JournalEntryLine
/// — the actual double-entry ledger, not a re-derivation from Transactions.
/// Every number here should reconcile with what TransactionService,
/// LoanController, and CashSessionController posted.
/// </summary>
[ApiController]
[Route("api/accounting")]
[Authorize(Policy = "AccountingView")]
public class AccountingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;

    public AccountingController(AppDbContext db, ICurrentUserService currentUser, IJournalPostingService journal)
    {
        _db = db;
        _currentUser = currentUser;
        _journal = journal;
    }

    private async Task LogAsync(string action, string? reportType = null, string? details = null)
    {
        _db.AccountingActivityLogs.Add(new AccountingActivityLog
        {
            CodeUser = _currentUser.CodeUser ?? "UNKNOWN", Action = action, ReportType = reportType, Details = details
        });
        await _db.SaveChangesAsync();
    }

    [HttpPost("log-action")]
    public async Task<ActionResult> LogAction([FromQuery] string action, [FromQuery] string? reportType)
    {
        await LogAsync(action, reportType, null);
        return Ok();
    }

    [HttpGet("activity-log")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> ActivityLog([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.AccountingActivityLogs.AsQueryable();
        if (from.HasValue) query = query.Where(l => l.ActionDate >= from.Value);
        if (to.HasValue) query = query.Where(l => l.ActionDate <= to.Value.AddDays(1).AddTicks(-1));
        var logs = await query.OrderByDescending(l => l.ActionDate).Take(300).ToListAsync();
        return Ok(logs.Select(l => new { l.AccountingActivityLogID, l.CodeUser, l.Action, l.ReportType, l.Details, l.ActionDate }));
    }

    [HttpGet("chart-of-accounts")]
    public async Task<ActionResult<IEnumerable<GLAccountDto>>> ChartOfAccounts()
    {
        var accounts = await _db.GLAccounts.OrderBy(a => a.Code).ToListAsync();
        return Ok(accounts.Select(a => new GLAccountDto(a.GLAccountID, a.Code, a.Name, a.Type.ToString(), a.NormalBalance, a.IsCashAccount)));
    }

    // ---- Trial Balance -----------------------------------------------------

    private async Task<List<TrialBalanceRowDto>> ComputeBalancesAsync(DateTime? from, DateTime? to, GLAccountType[]? types = null)
    {
        var accounts = await _db.GLAccounts.Where(a => a.Statut).ToListAsync();
        if (types != null) accounts = accounts.Where(a => types.Contains(a.Type)).ToList();

        var lineQuery = _db.JournalEntryLines.Include(l => l.JournalEntry).AsQueryable();
        if (from.HasValue) lineQuery = lineQuery.Where(l => l.JournalEntry!.EntryDate >= from.Value);
        if (to.HasValue) lineQuery = lineQuery.Where(l => l.JournalEntry!.EntryDate <= to.Value.AddDays(1).AddTicks(-1));
        var lines = await lineQuery.ToListAsync();

        var rows = new List<TrialBalanceRowDto>();
        foreach (var acc in accounts)
        {
            var accLines = lines.Where(l => l.GLAccountID == acc.GLAccountID).ToList();
            var debit = accLines.Sum(l => l.Debit);
            var credit = accLines.Sum(l => l.Credit);
            var balance = acc.NormalBalance == "DEBIT" ? debit - credit : credit - debit;
            if (debit == 0 && credit == 0) continue; // skip untouched accounts for a cleaner statement
            rows.Add(new TrialBalanceRowDto(acc.Code, acc.Name, acc.Type.ToString(), debit, credit, balance));
        }
        return rows.OrderBy(r => r.Code).ToList();
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceDto>> TrialBalance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await LogAsync("VIEW", "TrialBalance");
        var rows = await ComputeBalancesAsync(from, to);
        var totalDebit = rows.Sum(r => r.TotalDebit);
        var totalCredit = rows.Sum(r => r.TotalCredit);
        return Ok(new TrialBalanceDto(rows, totalDebit, totalCredit, Math.Abs(totalDebit - totalCredit) < 0.01m));
    }

    // ---- General Ledger ------------------------------------------------------

    [HttpGet("general-ledger/{glAccountId:int}")]
    public async Task<ActionResult<GeneralLedgerDto>> GeneralLedger(int glAccountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await LogAsync("VIEW", "GeneralLedger", $"GLAccountID={glAccountId}");
        var account = await _db.GLAccounts.FirstOrDefaultAsync(a => a.GLAccountID == glAccountId)
            ?? throw new KeyNotFoundException("Compte comptable introuvable.");

        var allLines = await _db.JournalEntryLines.Include(l => l.JournalEntry)
            .Where(l => l.GLAccountID == glAccountId)
            .OrderBy(l => l.JournalEntry!.EntryDate)
            .ToListAsync();

        var openingLines = from.HasValue ? allLines.Where(l => l.JournalEntry!.EntryDate < from.Value).ToList() : new List<JournalEntryLine>();
        var openingBalance = account.NormalBalance == "DEBIT"
            ? openingLines.Sum(l => l.Debit) - openingLines.Sum(l => l.Credit)
            : openingLines.Sum(l => l.Credit) - openingLines.Sum(l => l.Debit);

        var periodLines = allLines.Where(l =>
            (!from.HasValue || l.JournalEntry!.EntryDate >= from.Value) &&
            (!to.HasValue || l.JournalEntry!.EntryDate <= to.Value.AddDays(1).AddTicks(-1))
        ).ToList();

        var running = openingBalance;
        var result = new List<GeneralLedgerLineDto>();
        foreach (var line in periodLines)
        {
            running += account.NormalBalance == "DEBIT" ? (line.Debit - line.Credit) : (line.Credit - line.Debit);
            result.Add(new GeneralLedgerLineDto(
                line.JournalEntryID, line.JournalEntry!.EntryDate, line.JournalEntry.EntryNumber, line.JournalEntry.Description,
                line.JournalEntry.SourceType, line.JournalEntry.SourceReference, line.Debit, line.Credit, running
            ));
        }

        return Ok(new GeneralLedgerDto(account.Code, account.Name, openingBalance, result, running));
    }

    // ---- Balance Sheet ---------------------------------------------------

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetDto>> BalanceSheet([FromQuery] DateTime? asOf)
    {
        await LogAsync("VIEW", "BalanceSheet");
        var cutoff = asOf ?? DateTime.UtcNow;
        var assetRows = await ComputeBalancesAsync(null, cutoff, new[] { GLAccountType.ASSET });
        var liabilityRows = await ComputeBalancesAsync(null, cutoff, new[] { GLAccountType.LIABILITY });
        var equityRows = await ComputeBalancesAsync(null, cutoff, new[] { GLAccountType.EQUITY });

        // Net income to date rolls into equity (Retained Earnings) even though
        // no closing entry has physically moved it there yet.
        var revenueRows = await ComputeBalancesAsync(null, cutoff, new[] { GLAccountType.REVENUE });
        var expenseRows = await ComputeBalancesAsync(null, cutoff, new[] { GLAccountType.EXPENSE });
        var netIncome = revenueRows.Sum(r => r.Balance) - expenseRows.Sum(r => r.Balance);

        var assetsTotal = assetRows.Sum(r => r.Balance);
        var liabilitiesTotal = liabilityRows.Sum(r => r.Balance);
        var equityTotal = equityRows.Sum(r => r.Balance) + netIncome;

        return Ok(new BalanceSheetDto(
            cutoff,
            new BalanceSheetSectionDto("Actifs", assetRows, assetsTotal),
            new BalanceSheetSectionDto("Passifs", liabilityRows, liabilitiesTotal),
            new BalanceSheetSectionDto("Capitaux propres", equityRows, equityTotal),
            netIncome,
            Math.Abs(assetsTotal - (liabilitiesTotal + equityTotal)) < 0.01m
        ));
    }

    // ---- Profit & Loss -----------------------------------------------------

    [HttpGet("profit-loss")]
    public async Task<ActionResult<ProfitAndLossDto>> ProfitAndLoss([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await LogAsync("VIEW", "ProfitAndLoss");
        var start = from ?? new DateTime(DateTime.UtcNow.Year, 1, 1);
        var end = to ?? DateTime.UtcNow;

        var revenue = await ComputeBalancesAsync(start, end, new[] { GLAccountType.REVENUE });
        var expenses = await ComputeBalancesAsync(start, end, new[] { GLAccountType.EXPENSE });
        var totalRevenue = revenue.Sum(r => r.Balance);
        var totalExpenses = expenses.Sum(r => r.Balance);

        return Ok(new ProfitAndLossDto(start, end, revenue, totalRevenue, expenses, totalExpenses, totalRevenue - totalExpenses));
    }

    // ---- Cash Book -----------------------------------------------------

    [HttpGet("cash-book")]
    public async Task<ActionResult<CashBookDto>> CashBook([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await LogAsync("VIEW", "CashBook");
        var cashAccountIds = await _db.GLAccounts.Where(a => a.IsCashAccount).Select(a => a.GLAccountID).ToListAsync();

        var allLines = await _db.JournalEntryLines.Include(l => l.JournalEntry)
            .Where(l => cashAccountIds.Contains(l.GLAccountID))
            .OrderBy(l => l.JournalEntry!.EntryDate)
            .ToListAsync();

        var openingLines = from.HasValue ? allLines.Where(l => l.JournalEntry!.EntryDate < from.Value).ToList() : new List<JournalEntryLine>();
        var openingBalance = openingLines.Sum(l => l.Debit) - openingLines.Sum(l => l.Credit);

        var periodLines = allLines.Where(l =>
            (!from.HasValue || l.JournalEntry!.EntryDate >= from.Value) &&
            (!to.HasValue || l.JournalEntry!.EntryDate <= to.Value.AddDays(1).AddTicks(-1))
        ).ToList();

        var running = openingBalance;
        var result = new List<CashBookLineDto>();
        foreach (var line in periodLines)
        {
            running += line.Debit - line.Credit;
            result.Add(new CashBookLineDto(line.JournalEntry!.EntryDate, line.JournalEntry.EntryNumber, line.JournalEntry.Description, line.JournalEntry.SourceType, line.Debit, line.Credit, running));
        }

        return Ok(new CashBookDto(openingBalance, result, running, periodLines.Sum(l => l.Debit), periodLines.Sum(l => l.Credit)));
    }

    // ---- Cash Flow (direct method, by activity category) -------------------

    [HttpGet("cash-flow")]
    public async Task<ActionResult<CashFlowDto>> CashFlow([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        await LogAsync("VIEW", "CashFlow");
        var start = from ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = to ?? DateTime.UtcNow;

        var cashAccountIds = await _db.GLAccounts.Where(a => a.IsCashAccount).Select(a => a.GLAccountID).ToListAsync();
        var lines = await _db.JournalEntryLines.Include(l => l.JournalEntry)
            .Where(l => cashAccountIds.Contains(l.GLAccountID) && l.JournalEntry!.EntryDate >= start && l.JournalEntry!.EntryDate <= end.AddDays(1).AddTicks(-1))
            .ToListAsync();

        var beforeLines = await _db.JournalEntryLines.Include(l => l.JournalEntry)
            .Where(l => cashAccountIds.Contains(l.GLAccountID) && l.JournalEntry!.EntryDate < start)
            .ToListAsync();
        var openingCash = beforeLines.Sum(l => l.Debit) - beforeLines.Sum(l => l.Credit);

        decimal NetBySource(string sourceType) => lines.Where(l => l.JournalEntry!.SourceType == sourceType).Sum(l => l.Debit - l.Credit);

        var operating = NetBySource("TRANSACTION") + NetBySource("LOAN_REPAYMENT") + NetBySource("CASH_VARIANCE");
        var lending = NetBySource("LOAN_DISBURSEMENT");

        var cfLines = new List<CashFlowLineDto>
        {
            new("Activités d'exploitation (collectes, dépôts, retraits, remboursements, écarts)", operating),
            new("Activités de financement (décaissement de prêts)", lending),
        };
        var netChange = cfLines.Sum(l => l.Amount);

        return Ok(new CashFlowDto(start, end, openingCash, cfLines, netChange, openingCash + netChange));
    }

    // ---- Fiscal Period Closing ---------------------------------------------

    [HttpGet("periods")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> Periods()
    {
        var periods = await _db.AccountingPeriods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();
        return Ok(periods.Select(p => new { p.AccountingPeriodID, p.Year, p.Month, p.IsClosed, p.ClosedBy, p.ClosedDate }));
    }

    [HttpPost("periods/{year:int}/{month:int}/close")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> ClosePeriod(int year, int month)
    {
        var period = await _db.AccountingPeriods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month);
        if (period == null)
        {
            period = new AccountingPeriod { Year = year, Month = month };
            _db.AccountingPeriods.Add(period);
        }
        if (period.IsClosed) throw new InvalidOperationException("Cette période est déjà clôturée.");

        period.IsClosed = true;
        period.ClosedBy = _currentUser.CodeUser;
        period.ClosedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await LogAsync("CLOSE_PERIOD", null, $"{year}-{month:D2}");

        return Ok(new { message = $"Période {year}-{month:D2} clôturée. Aucune écriture manuelle ne pourra y être ajoutée." });
    }

    [HttpPost("periods/{year:int}/{month:int}/reopen")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> ReopenPeriod(int year, int month)
    {
        var period = await _db.AccountingPeriods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month)
            ?? throw new KeyNotFoundException("Période introuvable.");

        period.IsClosed = false;
        period.ClosedBy = null;
        period.ClosedDate = null;
        await _db.SaveChangesAsync();
        await LogAsync("REOPEN_PERIOD", null, $"{year}-{month:D2}");

        return Ok(new { message = $"Période {year}-{month:D2} rouverte." });
    }

    private async Task EnsurePeriodOpenAsync(DateTime date)
    {
        var closed = await _db.AccountingPeriods.AnyAsync(p => p.Year == date.Year && p.Month == date.Month && p.IsClosed);
        if (closed) throw new InvalidOperationException($"La période {date:yyyy-MM} est clôturée — aucune écriture ne peut y être ajoutée.");
    }

    // ---- Manual Journal Entries & Reversals (Maker-Checker) ----------------

    public record ManualEntryLineInput(int GLAccountID, decimal Debit, decimal Credit, string? Description);
    public record CreateManualEntryRequest(string EntryType, string Description, List<ManualEntryLineInput> Lines);

    [HttpGet("manual-entries")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> ManualEntries([FromQuery] string? status)
    {
        var query = _db.ManualJournalEntryDrafts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        var drafts = await query.OrderByDescending(d => d.RequestDate).Take(200).ToListAsync();
        return Ok(drafts.Select(d => new
        {
            d.ManualJournalEntryDraftID, d.EntryType, d.Description, d.Status,
            d.RequestedBy, d.RequestDate, d.ApprovedBy, d.ApprovalDate, d.RejectionReason,
            Lines = System.Text.Json.JsonSerializer.Deserialize<List<ManualEntryLineInput>>(d.LinesJson)
        }));
    }

    [HttpPost("manual-entries")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> CreateManualEntry(CreateManualEntryRequest request)
    {
        await EnsurePeriodOpenAsync(DateTime.UtcNow);

        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);
        if (request.Lines.Count < 2) throw new InvalidOperationException("Une écriture doit comporter au moins deux lignes.");
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            throw new InvalidOperationException($"L'écriture n'est pas équilibrée : Débit {totalDebit:N2} ≠ Crédit {totalCredit:N2}.");

        var draft = new ManualJournalEntryDraft
        {
            EntryType = request.EntryType,
            Description = request.Description,
            AgenceID = _currentUser.AgenceID ?? 0,
            LinesJson = System.Text.Json.JsonSerializer.Serialize(request.Lines),
            RequestedBy = _currentUser.CodeUser!
        };
        _db.ManualJournalEntryDrafts.Add(draft);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Écriture manuelle soumise pour approbation.", draftId = draft.ManualJournalEntryDraftID });
    }

    [HttpPost("manual-entries/{id:int}/approve")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> ApproveManualEntry(int id)
    {
        var draft = await _db.ManualJournalEntryDrafts.FirstOrDefaultAsync(d => d.ManualJournalEntryDraftID == id)
            ?? throw new KeyNotFoundException("Brouillon introuvable.");
        if (draft.Status != "PENDING") throw new InvalidOperationException("Ce brouillon a déjà été traité.");
        if (draft.RequestedBy == _currentUser.CodeUser)
            throw new InvalidOperationException("Vous ne pouvez pas approuver votre propre écriture (séparation des tâches).");

        await EnsurePeriodOpenAsync(DateTime.UtcNow);

        var lines = System.Text.Json.JsonSerializer.Deserialize<List<ManualEntryLineInput>>(draft.LinesJson)!;
        var postedId = await _journal.PostManualEntryAsync(
            draft.EntryType, draft.Description, draft.AgenceID, _currentUser.CodeUser!,
            lines.Select(l => (l.GLAccountID, l.Debit, l.Credit, l.Description)).ToList(),
            draft.ManualJournalEntryDraftID.ToString()
        );

        draft.Status = "APPROVED";
        draft.ApprovedBy = _currentUser.CodeUser;
        draft.ApprovalDate = DateTime.UtcNow;
        draft.PostedJournalEntryID = postedId;
        await _db.SaveChangesAsync();
        await LogAsync("POST_MANUAL_ENTRY", null, $"DraftID={id}, JournalEntryID={postedId}");

        return Ok(new { message = "Écriture manuelle approuvée et comptabilisée.", journalEntryId = postedId });
    }

    public record RejectManualEntryRequest(string Reason);

    [HttpPost("manual-entries/{id:int}/reject")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> RejectManualEntry(int id, RejectManualEntryRequest request)
    {
        var draft = await _db.ManualJournalEntryDrafts.FirstOrDefaultAsync(d => d.ManualJournalEntryDraftID == id)
            ?? throw new KeyNotFoundException("Brouillon introuvable.");
        if (draft.Status != "PENDING") throw new InvalidOperationException("Ce brouillon a déjà été traité.");

        draft.Status = "REJECTED";
        draft.RejectionReason = request.Reason;
        draft.ApprovedBy = _currentUser.CodeUser;
        draft.ApprovalDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Écriture manuelle rejetée." });
    }

    [HttpPost("entries/{journalEntryId:int}/reverse")]
    [Authorize(Policy = "AccountingAdmin")]
    public async Task<ActionResult> ReverseEntry(int journalEntryId)
    {
        await EnsurePeriodOpenAsync(DateTime.UtcNow);
        var reversalId = await _journal.ReverseEntryAsync(journalEntryId, _currentUser.CodeUser!);
        await LogAsync("REVERSE_ENTRY", null, $"Original={journalEntryId}, Reversal={reversalId}");
        return Ok(new { message = "Écriture contre-passée.", reversalJournalEntryId = reversalId });
    }
}
