namespace DailySavingV.API.Entities;

public enum GLAccountType { ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE }

/// <summary>
/// Chart of Accounts. A small, fixed set is seeded by the SQL patch — this is
/// intentionally not a full user-editable tree for now (see Accounting
/// Management notes): enough structure to produce correct Trial Balance,
/// Balance Sheet, P&L, and Cash Book/Flow.
/// </summary>
public class GLAccount
{
    public int GLAccountID { get; set; }
    public string Code { get; set; } = null!;      // e.g. "1010"
    public string Name { get; set; } = null!;
    public GLAccountType Type { get; set; }
    public string NormalBalance { get; set; } = null!; // DEBIT / CREDIT
    public bool IsCashAccount { get; set; }         // used by Cash Book / Cash Flow
    public bool Statut { get; set; } = true;
}

/// <summary>
/// One balanced double-entry posting. Auto-generated from real business
/// events (transactions, loan disbursement/repayment, cash variance) — there
/// is no manual journal-entry creation UI yet (see Accounting Management
/// limitations).
/// </summary>
public class JournalEntry
{
    public int JournalEntryID { get; set; }
    public string EntryNumber { get; set; } = null!;
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = null!;
    public string SourceType { get; set; } = null!;  // TRANSACTION / LOAN_DISBURSEMENT / LOAN_REPAYMENT / CASH_VARIANCE / LOAN_WRITE_OFF
    public string? SourceReference { get; set; }      // e.g. TransactionID, LoanID as string
    public int AgenceID { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class JournalEntryLine
{
    public int JournalEntryLineID { get; set; }
    public int JournalEntryID { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public int GLAccountID { get; set; }
    public GLAccount? GLAccount { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}
