namespace DailySavingV.API.Entities;

/// <summary>
/// One row per Agency. Defines the working days and business hours used to
/// validate session opening/closing times. Simplified from the full spec:
/// holidays are not modeled as a separate table yet (see integration notes).
/// </summary>
public class BusinessCalendar
{
    public int BusinessCalendarID { get; set; }
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    // Comma-separated ISO day numbers, e.g. "1,2,3,4,5" = Monday..Friday
    public string WorkingDays { get; set; } = "1,2,3,4,5,6";
    public TimeSpan OpeningTime { get; set; } = new(8, 0, 0);
    public TimeSpan ClosingTime { get; set; } = new(17, 0, 0);
    public int GracePeriodMinutes { get; set; } = 15;

    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class CashSession
{
    public int CashSessionID { get; set; }
    public string SessionNumber { get; set; } = null!; // e.g. CS-20260709-U001
    public string CodeUser { get; set; } = null!;
    public int AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public DateTime OpeningDate { get; set; } = DateTime.UtcNow;
    public decimal OpeningCash { get; set; }
    public decimal PreviousClosingCash { get; set; }
    public string? OpeningComment { get; set; }

    public DateTime? ClosingDate { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? PhysicalCash { get; set; }
    public decimal? CashDifference { get; set; }
    public string? ClosingComment { get; set; }
    public string? ClosedBy { get; set; }

    public string Status { get; set; } = "OPEN"; // OPEN / CLOSED
    public bool RequiresApproval { get; set; }
    public string? ApprovalStatus { get; set; }   // PENDING / APPROVED / REJECTED (only when RequiresApproval)
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }

    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class CashVariance
{
    public int CashVarianceID { get; set; }
    public int CashSessionID { get; set; }
    public CashSession? CashSession { get; set; }
    public decimal VarianceAmount { get; set; }
    public double VariancePercentage { get; set; }
    public string VarianceType { get; set; } = "SHORTAGE"; // SHORTAGE / OVERAGE
    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public string ApprovalStatus { get; set; } = "PENDING"; // PENDING / APPROVED / REJECTED
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
