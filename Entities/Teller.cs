namespace DailySavingV.API.Entities;

/// <summary>
/// One central cash holding per agency. Cash Sessions (per collector/cashier,
/// per day) already handle individual till reconciliation — the Vault is the
/// agency-level pool that supplies/receives cash to/from those tills.
/// </summary>
public class Vault
{
    public int VaultID { get; set; }
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }
    public decimal Balance { get; set; }
    public decimal? MinimumBalance { get; set; }
    public decimal? MaximumBalance { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CashMovement
{
    public int CashMovementID { get; set; }
    public string MovementNumber { get; set; } = null!;
    public int AgenceID { get; set; }

    public string MovementType { get; set; } = null!; // SUPPLY (vault->teller) / RETURN (teller->vault) / TRANSFER (teller->teller)
    public string? FromCodeUser { get; set; }  // null = the Vault itself
    public string? ToCodeUser { get; set; }    // null = the Vault itself

    public decimal Amount { get; set; }
    public string? Reason { get; set; }

    public string Status { get; set; } = "PENDING"; // PENDING / APPROVED / REJECTED / COMPLETED
    public string RequestedBy { get; set; } = null!;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
}
