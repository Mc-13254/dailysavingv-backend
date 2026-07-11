namespace DailySavingV.API.Entities;

public enum TransactionType
{
    DEPOSIT,
    WITHDRAWAL,
    DAILY_COLLECTION,
    LOAN_PAYMENT,
    TRANSFER,
    ACCOUNT_OPENING,
    ACCOUNT_CLOSING
}

public class Transactions
{
    public long TransactionID { get; set; }
    public TransactionType TransactionType { get; set; }

    public string AccountID { get; set; } = null!;
    public Accounts? Account { get; set; }
    public string ClientID { get; set; } = null!;
    public Client? Client { get; set; }
    public string? CollectorID { get; set; }
    public int? CashSessionID { get; set; }
    public CashSession? CashSession { get; set; }

    // Only populated for TRANSFER: the other leg of the movement.
    public string? ToAccountID { get; set; }
    public Accounts? ToAccount { get; set; }
    public string? ToClientID { get; set; }
    public long? LinkedTransactionID { get; set; } // pairs the debit (sender) and credit (receiver) rows of one transfer

    // Agency-scoping key
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public decimal Montant { get; set; }

    public int? CommissionTypeID { get; set; }
    public int? CommissionRangeID { get; set; }
    public decimal MontantCommission { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }

    // Bank-receipt style fields
    public string? RemitterName { get; set; }    // who is handing over the money (depositor / transfer sender)
    public string? BeneficiaryName { get; set; } // who the money is for (account holder / transfer receiver)
    public string? PaymentMethod { get; set; }   // CASH / MOBILE_MONEY / BANK_TRANSFER / CHEQUE
    public string? ReferenceNumber { get; set; }
    public string? Comment { get; set; }

    // Cash-only: bill/coin breakdown as JSON, e.g. {"10000":2,"5000":1,"500":4}.
    // Kept as JSON rather than a child table since it's purely informational —
    // the authoritative amount is always Montant; this just proves how it was counted.
    public string? CashBreakdownJson { get; set; }

    public string? ReceiptNumber { get; set; }
    public DateTime DateTransaction { get; set; } = DateTime.UtcNow;
    public string Statut { get; set; } = "VALIDATED";   // VALIDATED / REVERSED / CANCELLED

    public string? CreatedBy { get; set; }
    public string? ValidatedBy { get; set; }
    public DateTime? ValidationDate { get; set; }
}

public class TransactionReversalRequest
{
    public int TransactionReversalRequestID { get; set; }
    public string ClientID { get; set; } = null!;
    public string? CollectorID { get; set; }
    public decimal Montant { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "PENDING"; // PENDING / APPROVED / REJECTED / COMPLETED
    public string RequestedBy { get; set; } = null!;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }

    // Only filled in once APPROVED — the maker then looks up the exact
    // transaction and executes the reversal immediately, no further approval.
    public long? TransactionID { get; set; }
    public string? ExecutedBy { get; set; }
    public DateTime? ExecutionDate { get; set; }
}

public class HistTransactions
{
    public long HistTransactionID { get; set; }
    public long TransactionID { get; set; }
    public string Action { get; set; } = null!;   // CREATE / VALIDATE / REVERSE / CANCEL
    public string? PreviousData { get; set; }     // JSON snapshot
    public string? NewData { get; set; }          // JSON snapshot
    public string? ActionBy { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}

public class HistCalculComis
{
    public long HistCalculComisID { get; set; }
    public long TransactionID { get; set; }
    public int CommissionTypeID { get; set; }
    public int CommissionRangeID { get; set; }
    public decimal MontantTransaction { get; set; }
    public string CalculationMethod { get; set; } = null!;
    public decimal TauxAppliqueOuFixe { get; set; }
    public decimal MontantCommission { get; set; }
    public DateTime DateCalcul { get; set; } = DateTime.UtcNow;
}

public class Activite
{
    public long ActiviteID { get; set; }
    public string? CodeUser { get; set; }
    public string Action { get; set; } = null!;
    public string? Module { get; set; }
    public string? Description { get; set; }
    public string? AdresseIP { get; set; }
    public DateTime DateAction { get; set; } = DateTime.UtcNow;
}
