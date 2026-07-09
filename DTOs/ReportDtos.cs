namespace DailySavingV.API.DTOs;

public record TransactionHistoryFilter(
    string? Search, string? TransactionType, string? Status, string? PaymentMethod,
    int? AgenceID, string? CollectorID, DateTime? From, DateTime? To
);

public record TransactionHistoryRowDto(
    long TransactionID, string? ReceiptNumber, string TransactionType,
    string AccountID, string? ToAccountID, string ClientID, string ClientName,
    string? CollectorID, string? CollectorName, string AgenceNom,
    decimal Montant, decimal MontantCommission, string? PaymentMethod,
    string Statut, DateTime DateTransaction
);

public record TransactionHistoryDetailDto(
    long TransactionID, string? ReceiptNumber, string TransactionType,
    string AccountID, string? AccountType, string? ToAccountID,
    string ClientID, string ClientName, string? ToClientID, string? ToClientName,
    string? CollectorID, string? CollectorName,
    string? ContractNumber, int AgenceID, string AgenceNom,
    int? CashSessionID, string? CashSessionNumber,
    decimal Montant, decimal OpeningBalance, decimal ClosingBalance, decimal MontantCommission,
    string? RemitterName, string? BeneficiaryName, string? PaymentMethod,
    string? ReferenceNumber, string? Comment, string? CashBreakdownJson,
    string Statut, DateTime DateTransaction,
    string? CreatedBy, string? ValidatedBy, DateTime? ValidationDate
);

public record TransactionHistoryStatsDto(
    int TodayCount, decimal TodayAmount,
    decimal TotalCollections, decimal TotalDeposits, decimal TotalWithdrawals, decimal TotalTransfers,
    int PendingCount, int ValidatedCount, int RejectedCount
);

public record ReportCenterCardDto(string Key, string Label, int Count, decimal? Amount);

// ---- Cash Session Reports ----

public record CashSessionReportRowDto(
    int CashSessionID, string SessionNumber, string CodeUser, string UserFullName, string AgenceNom,
    DateTime OpeningDate, DateTime? ClosingDate, decimal OpeningCash,
    decimal? ExpectedCash, decimal? PhysicalCash, decimal? CashDifference,
    decimal Collections, decimal Deposits, decimal Withdrawals, decimal Transfers,
    string Status, bool RequiresApproval, string? ApprovalStatus
);

public record CashSessionReportDetailDto(
    int CashSessionID, string SessionNumber, string CodeUser, string UserFullName, int AgenceID, string AgenceNom,
    DateTime OpeningDate, decimal OpeningCash, decimal PreviousClosingCash, string? OpeningComment,
    DateTime? ClosingDate, decimal? ExpectedCash, decimal? PhysicalCash, string? PhysicalCashBreakdownJson,
    decimal? CashDifference, string? ClosingComment, string? ClosedBy,
    decimal Collections, decimal Deposits, decimal Withdrawals, decimal Transfers, decimal Commission,
    int TransactionCount,
    string Status, bool RequiresApproval, string? ApprovalStatus, string? ApprovedBy, DateTime? ApprovalDate,
    string? VarianceReason, string? VarianceComment
);

public record CashSessionReportStatsDto(
    int TodaySessions, int OpenSessions, int ClosedSessions,
    int BalancedSessions, int UnbalancedSessions,
    decimal TotalExpectedCash, decimal TotalPhysicalCash, decimal TotalVariance,
    double AverageSessionDurationHours
);
