using DailySavingV.API.Data;
using DailySavingV.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

public interface IJournalPostingService
{
    Task PostTransactionAsync(Transactions tx);
    Task PostLoanDisbursementAsync(Loan loan);
    Task PostLoanRepaymentAsync(Loan loan, decimal principalPaid, decimal interestPaid, decimal penaltyPaid, string reference);
    Task PostLoanWriteOffAsync(Loan loan);
    Task PostCashVarianceAsync(int agenceId, int cashSessionId, decimal variance, string reference);
}

/// <summary>
/// Generates balanced (debit == credit) journal entries from real business
/// events. This is intentionally rule-based and narrow — it covers the
/// transaction types that actually exist in this system, not a generic
/// posting engine. See Accounting Management notes for what's out of scope
/// (manual journal entries, multi-currency, accrual adjustments).
/// </summary>
public class JournalPostingService : IJournalPostingService
{
    private readonly AppDbContext _db;
    private int _sequenceCounter = -1;

    public JournalPostingService(AppDbContext db)
    {
        _db = db;
    }

    private async Task<int> GLAsync(string code)
    {
        var account = await _db.GLAccounts.FirstOrDefaultAsync(a => a.Code == code)
            ?? throw new InvalidOperationException($"Compte comptable '{code}' introuvable — le plan comptable n'est peut-être pas initialisé.");
        return account.GLAccountID;
    }

    private async Task<string> NextEntryNumberAsync()
    {
        if (_sequenceCounter < 0) _sequenceCounter = await _db.JournalEntries.CountAsync();
        _sequenceCounter++;
        return $"JE-{DateTime.UtcNow:yyyyMMdd}-{_sequenceCounter:D6}";
    }

    /// Creates the header + balanced lines. Throws if debits != credits — a
    /// silent accounting error is worse than a loud one.
    private async Task PostAsync(string sourceType, string? sourceReference, int agenceId, string description, string createdBy, List<(int glAccountId, decimal debit, decimal credit, string? lineDesc)> lines)
    {
        var totalDebit = lines.Sum(l => l.debit);
        var totalCredit = lines.Sum(l => l.credit);
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            throw new InvalidOperationException($"Écriture comptable déséquilibrée pour {sourceType} {sourceReference} : Débit {totalDebit:N2} != Crédit {totalCredit:N2}.");

        var entry = new JournalEntry
        {
            EntryNumber = await NextEntryNumberAsync(),
            Description = description,
            SourceType = sourceType,
            SourceReference = sourceReference,
            AgenceID = agenceId,
            CreatedBy = createdBy
        };
        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();

        foreach (var (glAccountId, debit, credit, lineDesc) in lines)
        {
            _db.JournalEntryLines.Add(new JournalEntryLine
            {
                JournalEntryID = entry.JournalEntryID,
                GLAccountID = glAccountId,
                Debit = debit,
                Credit = credit,
                Description = lineDesc
            });
        }
        await _db.SaveChangesAsync();
    }

    public async Task PostTransactionAsync(Transactions tx)
    {
        var cash = await GLAsync("1010");
        var deposits = await GLAsync("2010");
        var commissionIncome = await GLAsync("4020");
        var createdBy = tx.CreatedBy ?? "SYSTEM";

        switch (tx.TransactionType)
        {
            case TransactionType.DAILY_COLLECTION:
            case TransactionType.DEPOSIT:
            {
                // Cash comes in. The commission (if any) is the institution's
                // revenue, taken out of what's actually credited to the client's
                // savings balance.
                var netToSavings = tx.Montant - tx.MontantCommission;
                var lines = new List<(int, decimal, decimal, string?)>
                {
                    (cash, tx.Montant, 0, "Espèces reçues"),
                    (deposits, 0, netToSavings, "Crédit dépôt client"),
                };
                if (tx.MontantCommission > 0) lines.Add((commissionIncome, 0, tx.MontantCommission, "Commission"));
                await PostAsync("TRANSACTION", tx.TransactionID.ToString(), tx.AgenceID,
                    $"{tx.TransactionType} — {tx.ReceiptNumber}", createdBy, lines);
                break;
            }
            case TransactionType.WITHDRAWAL:
            {
                var lines = new List<(int, decimal, decimal, string?)>
                {
                    (deposits, tx.Montant, 0, "Débit dépôt client"),
                    (cash, 0, tx.Montant - tx.MontantCommission, "Espèces remises"),
                };
                if (tx.MontantCommission > 0) lines.Add((commissionIncome, 0, tx.MontantCommission, "Commission"));
                await PostAsync("TRANSACTION", tx.TransactionID.ToString(), tx.AgenceID,
                    $"WITHDRAWAL — {tx.ReceiptNumber}", createdBy, lines);
                break;
            }
            case TransactionType.TRANSFER:
                // Net zero on the aggregate Client Deposits account (money moves
                // from one client's balance to another's, both within the same
                // GL account) — no journal entry needed at the chart-of-accounts
                // level. Per-client subledger detail is not modeled here.
                break;
            default:
                break; // LOAN_PAYMENT is posted via PostLoanRepaymentAsync instead, using real allocation
        }
    }

    public async Task PostLoanDisbursementAsync(Loan loan)
    {
        var cash = await GLAsync("1010");
        var deposits = await GLAsync("2010");
        var loansReceivable = await GLAsync("1100");

        // If disbursed straight to a client's savings account, the credit side
        // is the deposit liability increasing; otherwise it's cash leaving the till.
        var creditAccount = !string.IsNullOrWhiteSpace(loan.DisbursedToAccountID) ? deposits : cash;

        await PostAsync("LOAN_DISBURSEMENT", loan.LoanID.ToString(), loan.AgenceID,
            $"Décaissement prêt {loan.LoanNumber}", loan.DisbursedBy,
            new List<(int, decimal, decimal, string?)>
            {
                (loansReceivable, loan.PrincipalAmount, 0, "Principal décaissé"),
                (creditAccount, 0, loan.PrincipalAmount, "Sortie de fonds"),
            });
    }

    public async Task PostLoanRepaymentAsync(Loan loan, decimal principalPaid, decimal interestPaid, decimal penaltyPaid, string reference)
    {
        var total = principalPaid + interestPaid + penaltyPaid;
        if (total <= 0) return;

        var cash = await GLAsync("1010");
        var loansReceivable = await GLAsync("1100");
        var interestIncome = await GLAsync("4010");
        var penaltyIncome = await GLAsync("4030");

        var lines = new List<(int, decimal, decimal, string?)> { (cash, total, 0, "Remboursement reçu") };
        if (principalPaid > 0) lines.Add((loansReceivable, 0, principalPaid, "Remboursement principal"));
        if (interestPaid > 0) lines.Add((interestIncome, 0, interestPaid, "Intérêt perçu"));
        if (penaltyPaid > 0) lines.Add((penaltyIncome, 0, penaltyPaid, "Pénalité perçue"));

        await PostAsync("LOAN_REPAYMENT", reference, loan.AgenceID,
            $"Remboursement prêt {loan.LoanNumber}", "SYSTEM", lines);
    }

    public async Task PostLoanWriteOffAsync(Loan loan)
    {
        var writeOffExpense = await GLAsync("5010");
        var loansReceivable = await GLAsync("1100");
        var amount = loan.OutstandingPrincipal + loan.OutstandingInterest;
        if (amount <= 0) return;

        await PostAsync("LOAN_WRITE_OFF", loan.LoanID.ToString(), loan.AgenceID,
            $"Passage en perte — prêt {loan.LoanNumber}", "SYSTEM",
            new List<(int, decimal, decimal, string?)>
            {
                (writeOffExpense, amount, 0, "Perte constatée"),
                (loansReceivable, 0, amount, "Sortie de la créance"),
            });
    }

    public async Task PostCashVarianceAsync(int agenceId, int cashSessionId, decimal variance, string reference)
    {
        if (Math.Abs(variance) < 0.01m) return; // balanced session, nothing to post

        var cash = await GLAsync("1010");
        var overShortExpense = await GLAsync("5030");

        // Positive variance (physical > expected) = unexplained surplus, a credit
        // to the expense account (reduces net expense). Negative = shortage, a debit.
        List<(int, decimal, decimal, string?)> lines = variance < 0
            ? new() { (overShortExpense, -variance, 0, "Manquant de caisse"), (cash, 0, -variance, "Ajustement caisse") }
            : new() { (cash, variance, 0, "Ajustement caisse"), (overShortExpense, 0, variance, "Excédent de caisse") };

        await PostAsync("CASH_VARIANCE", reference, agenceId, $"Écart de caisse — session #{cashSessionId}", "SYSTEM", lines);
    }
}
