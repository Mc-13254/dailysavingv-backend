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

// ---- Client Reports ----

public record ClientReportRowDto(
    string ClientID, string ClientName, string? PhoneNumber, string AgenceNom,
    string? CollectorName, int AccountCount, decimal TotalBalance, int ContractCount,
    int CollectionCount, decimal CollectionAmount, DateTime? LastTransactionDate,
    string ValidationStatus, bool IsBlacklisted, DateTime CreatedDate
);

public record ClientReportDetailDto(
    string ClientID, string ClientName, string? PhoneNumber, string? Email, string? Address,
    string? Sexe, DateTime? DateOfBirth, string? Nationality, string? Occupation,
    string AgenceNom, string? CollectorName, string? ZoneNom,
    string ValidationStatus, bool IsBlacklisted, string? RiskLevel, DateTime CreatedDate,
    List<ClientAccountSummaryDto> Accounts,
    List<ClientContractSummaryDto> Contracts,
    decimal TotalCollections, decimal TotalDeposits, decimal TotalWithdrawals, decimal TotalTransfers,
    int TransactionCount, DateTime? LastTransactionDate
);

public record ClientAccountSummaryDto(string AccountID, string? AccountType, decimal Balance, string Status);
public record ClientContractSummaryDto(int ContractID, string ContractNumber, string? ContractTypeName, string Statut, DateTime? StartDate);

public record ClientReportStatsDto(
    int TotalClients, int ActiveClients, int PendingClients, int BlockedClients,
    int NewThisMonth, int ClientsWithAccounts, int ClientsWithContracts
);

// ---- Account Reports ----

public record AccountReportRowDto(
    string AccountID, string? AccountType, string ClientID, string ClientName,
    string AgenceNom, string? CollectorName, string? ContractNumber,
    decimal Balance, decimal AvailableBalance, string Status, DateTime CreateDate, DateTime? LastTransactionDate
);

public record AccountReportDetailDto(
    string AccountID, string? AccountType, string Currency, string ClientID, string ClientName,
    string AgenceNom, string? CollectorName, string? ContractNumber, string? ContractTypeName,
    decimal OpeningBalance, decimal Balance, decimal AvailableBalance, decimal BlockedBalance,
    decimal? MinimumBalance, decimal? MaximumBalance,
    decimal TotalCollections, decimal TotalDeposits, decimal TotalWithdrawals, decimal TotalTransfers,
    int TransactionCount, DateTime CreateDate, DateTime? LastTransactionDate,
    string Status, string? FreezeReason, string? CloseReason
);

public record AccountReportStatsDto(
    int TotalAccounts, int ActiveAccounts, int FrozenAccounts, int ClosedAccounts, int DormantAccounts,
    decimal TotalBalance, decimal AverageBalance, int NewThisMonth
);

// ---- Contract Reports ----

public record ContractReportRowDto(
    int ContractID, string ContractNumber, string ClientID, string ClientName,
    string AgenceNom, string? CollectorName, string? ContractTypeName, string? CommissionTypeName,
    decimal? AccountBalance, decimal CommissionGenerated, string Statut, DateTime? StartDate, DateTime? EndDate
);

public record ContractReportDetailDto(
    int ContractID, string ContractNumber, string ClientID, string ClientName,
    string AgenceNom, string? CollectorName, string? ContractTypeName, string? CommissionTypeName,
    DateTime? StartDate, DateTime? EndDate, string Statut, string? TerminationReason, DateTime? TerminationDate,
    string? AccountID, decimal? AccountBalance,
    decimal TotalCollected, int CollectionCount, decimal AverageCollection,
    decimal CommissionGenerated, decimal? EstimatedProfitability
);

public record ContractReportStatsDto(
    int TotalContracts, int ActiveContracts, int TerminatedContracts,
    int ExpiringSoon, decimal TotalCommissionGenerated
);

// ---- Commission Reports ----

public record CommissionReportRowDto(
    long TransactionID, string? ReceiptNumber, string? CollectorID, string? CollectorName,
    string AgenceNom, string ClientID, string ClientName, string? ContractNumber,
    string? CommissionTypeName, decimal Montant, decimal MontantCommission, DateTime DateTransaction
);

public record CommissionByGroupDto(string Label, decimal CommissionAmount, decimal CollectionAmount, int Count);

public record CommissionReportStatsDto(
    decimal TotalCommission, decimal TodayCommission, decimal MonthlyCommission, decimal YearlyCommission,
    decimal AverageCommission, decimal HighestCommission, decimal LowestCommission
);

// ---- Agency Reports ----

public record AgencyReportRowDto(
    int AgenceID, string CodeAgence, string Nom, string? ManagerName,
    int CollectorCount, int ClientCount, int AccountCount,
    decimal Collections, decimal Deposits, decimal Withdrawals, decimal Transfers, decimal Commission,
    decimal CashVarianceTotal, int Rank
);

public record AgencyReportDetailDto(
    int AgenceID, string CodeAgence, string Nom, string? Address, string? PrimaryPhone, string? Email,
    string? ManagerName, int CollectorCount, int CashierCount, int ClientCount, int AccountCount, int ContractCount,
    decimal Collections, decimal Deposits, decimal Withdrawals, decimal Transfers, decimal Commission,
    decimal TotalBalance, int OpenSessions, int ClosedSessions, decimal CashVarianceTotal,
    int Rank, int TotalAgencies
);

public record AgencyReportStatsDto(
    int TotalAgencies, string? TopAgencyName, string? LowestAgencyName,
    decimal TotalRevenue, decimal TotalCommission, int TotalClients, int TotalCollectors
);
