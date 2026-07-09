namespace DailySavingV.API.DTOs;

public record LoanProductDto(
    int LoanProductID, string Code, string Name, string InterestMethod, decimal AnnualInterestRate,
    decimal MinAmount, decimal MaxAmount, int MinTermMonths, int MaxTermMonths,
    decimal PenaltyRatePerDay, int GracePeriodDays, string Statut
);
public record CreateLoanProductRequest(
    string Code, string Name, string InterestMethod, decimal AnnualInterestRate,
    decimal MinAmount, decimal MaxAmount, int MinTermMonths, int MaxTermMonths,
    decimal PenaltyRatePerDay, int GracePeriodDays
);

public record CreateLoanApplicationRequest(string ClientID, int LoanProductID, decimal RequestedAmount, int RequestedTermMonths, string? Purpose);
public record ApproveLoanApplicationRequest(decimal ApprovedAmount, int ApprovedTermMonths);
public record RejectLoanApplicationRequest(string Reason);
public record DisburseLoanRequest(string? DisbursedToAccountID);

public record LoanApplicationRowDto(
    int LoanApplicationID, string ClientID, string ClientName, string ProductName,
    decimal RequestedAmount, int RequestedTermMonths, decimal? ApprovedAmount, int? ApprovedTermMonths,
    string Status, string RequestedBy, DateTime RequestDate, string? ApprovedBy, DateTime? ApprovalDate
);

public record LoanInstallmentDto(
    int LoanInstallmentID, int InstallmentNumber, DateTime DueDate,
    decimal PrincipalDue, decimal InterestDue, decimal PenaltyDue, decimal TotalDue,
    decimal PrincipalPaid, decimal InterestPaid, decimal PenaltyPaid, decimal TotalPaid,
    DateTime? PaidDate, string Status
);

public record LoanRowDto(
    int LoanID, string LoanNumber, string ClientID, string ClientName, string ProductName,
    decimal PrincipalAmount, decimal OutstandingPrincipal, decimal OutstandingInterest, decimal OutstandingPenalty,
    DateTime DisbursementDate, DateTime? NextDueDate, string Status, int OverdueInstallments
);

public record LoanDetailDto(
    int LoanID, string LoanNumber, string ClientID, string ClientName, string AgenceNom,
    string ProductName, decimal AnnualInterestRate, string InterestMethod,
    decimal PrincipalAmount, int TermMonths, DateTime DisbursementDate, string DisbursedBy, string? DisbursedToAccountID,
    decimal TotalPrincipal, decimal TotalInterest, decimal OutstandingPrincipal, decimal OutstandingInterest, decimal OutstandingPenalty,
    DateTime? NextDueDate, string Status, string? WriteOffReason,
    List<LoanInstallmentDto> Installments, List<LoanRepaymentDto> Repayments
);

public record LoanRepaymentDto(int LoanRepaymentID, decimal Amount, decimal PrincipalPaid, decimal InterestPaid, decimal PenaltyPaid, string ReceiptNumber, DateTime PaymentDate, string ReceivedBy);

public record RepayLoanRequest(decimal Amount);
public record WriteOffLoanRequest(string Reason);

public record LoanDashboardDto(
    int TotalLoans, int ActiveLoans, int ClosedLoans, int OverdueLoans,
    decimal TotalDisbursed, decimal TotalOutstandingPrincipal, decimal TotalOutstandingInterest,
    decimal DisbursedThisMonth, int PendingApplications
);
