namespace DailySavingV.API.Entities;

/// <summary>
/// A request to reverse a completed Deposit/Withdrawal/Transfer/Daily
/// Collection — never applied directly. Approval creates a mirrored
/// reversing Transactions row (restoring balances) and a mirrored accounting
/// reversal entry, and marks the original transaction REVERSED.
/// </summary>
public class TransactionReversalRequest
{
    public int TransactionReversalRequestID { get; set; }
    public long TransactionID { get; set; }
    public Transactions? Transaction { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "PENDING"; // PENDING / APPROVED / REJECTED
    public string RequestedBy { get; set; } = null!;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
    public long? ReversalTransactionID { get; set; } // the mirrored transaction created once approved
}
