namespace DailySavingV.API.Entities;

public class LoanProduct
{
    public int LoanProductID { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string InterestMethod { get; set; } = "REDUCING"; // REDUCING / FLAT
    public decimal AnnualInterestRate { get; set; } // percent, e.g. 24 = 24%/year
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public int MinTermMonths { get; set; }
    public int MaxTermMonths { get; set; }
    public decimal PenaltyRatePerDay { get; set; } // percent of overdue installment per day late
    public int GracePeriodDays { get; set; } = 0;
    public string Statut { get; set; } = "ACTIVE"; // ACTIVE / INACTIVE
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class LoanApplication
{
    public int LoanApplicationID { get; set; }
    public string ClientID { get; set; } = null!;
    public Client? Client { get; set; }
    public int LoanProductID { get; set; }
    public LoanProduct? LoanProduct { get; set; }
    public int AgenceID { get; set; }
    public string? CollectorID { get; set; }

    public decimal RequestedAmount { get; set; }
    public int RequestedTermMonths { get; set; }
    public string? Purpose { get; set; }

    // Guarantor — standard banking due-diligence requirement
    public string? GuarantorName { get; set; }
    public string? GuarantorPhone { get; set; }
    public string? GuarantorAddress { get; set; }
    public string? GuarantorIDNumber { get; set; }
    public string? GuarantorPhotoUrl { get; set; }
    public string? GuarantorSignatureUrl { get; set; }
    public string? CollateralDescription { get; set; }

    public decimal? ApprovedAmount { get; set; }
    public int? ApprovedTermMonths { get; set; }

    public string Status { get; set; } = "PENDING"; // PENDING / APPROVED / REJECTED / DISBURSED
    public string RequestedBy { get; set; } = null!;
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public string? RejectionReason { get; set; }
}

public class Loan
{
    public int LoanID { get; set; }
    public int LoanApplicationID { get; set; }
    public LoanApplication? LoanApplication { get; set; }
    public string LoanNumber { get; set; } = null!;
    public string ClientID { get; set; } = null!;
    public Client? Client { get; set; }
    public int LoanProductID { get; set; }
    public LoanProduct? LoanProduct { get; set; }
    public int AgenceID { get; set; }
    public string? CollectorID { get; set; }

    public decimal PrincipalAmount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public string InterestMethod { get; set; } = "REDUCING";
    public int TermMonths { get; set; }

    public DateTime DisbursementDate { get; set; } = DateTime.UtcNow;
    public string? DisbursedToAccountID { get; set; }
    public string DisbursedBy { get; set; } = null!;

    public decimal TotalPrincipal { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public decimal OutstandingInterest { get; set; }
    public decimal OutstandingPenalty { get; set; }

    public DateTime? NextDueDate { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE / CLOSED / WRITTEN_OFF / RESTRUCTURED
    public string? WriteOffReason { get; set; }
    public DateTime? ClosedDate { get; set; }
}

public class LoanInstallment
{
    public int LoanInstallmentID { get; set; }
    public int LoanID { get; set; }
    public Loan? Loan { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }

    public decimal PrincipalDue { get; set; }
    public decimal InterestDue { get; set; }
    public decimal PenaltyDue { get; set; }

    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal PenaltyPaid { get; set; }

    public DateTime? PaidDate { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING / PARTIAL / PAID / OVERDUE
}

public class LoanRepayment
{
    public int LoanRepaymentID { get; set; }
    public int LoanID { get; set; }
    public Loan? Loan { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalPaid { get; set; }
    public decimal InterestPaid { get; set; }
    public decimal PenaltyPaid { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string ReceivedBy { get; set; } = null!;
}
