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

/// <summary>Monthly fiscal period lock. Closing a period blocks new manual
/// entries dated inside it — automatic postings are always "today" so a
/// closed past period is naturally protected from them too.</summary>
public class AccountingPeriod
{
    public int AccountingPeriodID { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsClosed { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime? ClosedDate { get; set; }
}

/// <summary>
/// Manual journal entries and reversals never post directly — they sit here
/// as a draft awaiting a second, different, authorized user's approval
/// (Maker-Checker), then a balanced JournalEntry is created from ApprovedBy.
/// </summary>
public class ManualJournalEntryDraft
{
    public int ManualJournalEntryDraftID { get; set; }
    public string EntryType { get; set; } = "MANUAL"; // MANUAL / REVERSAL / ADJUSTMENT
    public string Description { get; set; } = null!;
    public int? ReversalOfJournalEntryID { get; set; }
    public int AgenceID { get; set; }
    public string LinesJson { get; set; } = null!; // [{glAccountId, debit, credit, description}]
    public string Status { get; set; } = "PENDING"; // PENDING / APPROVED / REJECTED
    public string RequestedBy { get; set; } = null!;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
    public int? PostedJournalEntryID { get; set; }
}

/// <summary>Records every view/generate/export/print/close/post action on the
/// Accounting module specifically — narrower than a system-wide Activity Log,
/// but real and queryable, matching the "every action is recorded" requirement
/// for this module.</summary>
public class AccountingActivityLog
{
    public int AccountingActivityLogID { get; set; }
    public string CodeUser { get; set; } = null!;
    public string Action { get; set; } = null!; // VIEW / EXPORT / PRINT / CLOSE_PERIOD / REOPEN_PERIOD / POST_MANUAL_ENTRY / REVERSE_ENTRY
    public string? ReportType { get; set; }
    public string? Details { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}
