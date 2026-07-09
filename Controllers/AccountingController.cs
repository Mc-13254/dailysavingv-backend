using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
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
[Authorize(Policy = "SupervisorOrAdmin")]
public class AccountingController : ControllerBase
{
    private readonly AppDbContext _db;

    public AccountingController(AppDbContext db)
    {
        _db = db;
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
        var rows = await ComputeBalancesAsync(from, to);
        var totalDebit = rows.Sum(r => r.TotalDebit);
        var totalCredit = rows.Sum(r => r.TotalCredit);
        return Ok(new TrialBalanceDto(rows, totalDebit, totalCredit, Math.Abs(totalDebit - totalCredit) < 0.01m));
    }

    // ---- General Ledger ------------------------------------------------------

    [HttpGet("general-ledger/{glAccountId:int}")]
    public async Task<ActionResult<GeneralLedgerDto>> GeneralLedger(int glAccountId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
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
                line.JournalEntry!.EntryDate, line.JournalEntry.EntryNumber, line.JournalEntry.Description,
                line.JournalEntry.SourceType, line.JournalEntry.SourceReference, line.Debit, line.Credit, running
            ));
        }

        return Ok(new GeneralLedgerDto(account.Code, account.Name, openingBalance, result, running));
    }

    // ---- Balance Sheet ---------------------------------------------------

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetDto>> BalanceSheet([FromQuery] DateTime? asOf)
    {
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
}
