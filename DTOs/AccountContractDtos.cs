namespace DailySavingV.API.DTOs;

// ============================== ACCOUNTS ==============================

public record AccountFullDto(
    string AccountID, string ClientID, string? ClientName, string? NumCarnet,
    int? ContractID, string? ContractNumber, string? CollectorID, string? CollectorName,
    string AccountType, string Currency,
    decimal OpeningBalance, decimal Balance, decimal AvailableBalance, decimal BlockedBalance,
    decimal? MinimumBalance, decimal? MaximumBalance,
    decimal? DailyDepositLimit, decimal? DailyWithdrawalLimit, decimal? DailyTransactionLimit,
    bool OverdraftAllowed, decimal? OverdraftLimit,
    string Status, bool Active, int AgenceID, string? CreatedBy, DateTime CreateDate
);

public record CreateAccountRequest(
    string ClientID, int ContractID, string? NumCarnet, string AccountType, string Currency,
    decimal OpeningBalance, decimal? MinimumBalance, decimal? MaximumBalance,
    decimal? DailyDepositLimit, decimal? DailyWithdrawalLimit, decimal? DailyTransactionLimit,
    bool OverdraftAllowed, decimal? OverdraftLimit
);

public record UpdateAccountRequest(
    decimal? MinimumBalance, decimal? MaximumBalance,
    decimal? DailyDepositLimit, decimal? DailyWithdrawalLimit, decimal? DailyTransactionLimit,
    bool? OverdraftAllowed, decimal? OverdraftLimit, string? Status
);

public record FreezeAccountRequest(string Reason);
public record CloseAccountRequest(string Reason);

public record StatementLineDto(DateTime Date, string Type, string? Description, decimal Amount, decimal RunningBalance);
public record AccountStatementDto(
    string AccountID, string ClientName, decimal OpeningBalance, decimal ClosingBalance,
    decimal TotalDeposits, decimal TotalWithdrawals, decimal TotalCollections,
    List<StatementLineDto> Lines
);

// ============================== CONTRACTS ==============================

public record ContractFullDto(
    int ContractID, string ContractNumber, string? ClientID, string? ClientName,
    int? AgenceID, string? CollectorID, string? CollectorName,
    int? CommissionTypeID, string? CommissionTypeName, int? CommissionRangeID,
    string CollectionFrequency, string? CollectionDay,
    decimal? OpeningDeposit, decimal? MinimumBalance, decimal? MaximumBalance,
    string? PenaltyRules, int? GracePeriod,
    DateTime StartDate, DateTime? EndDate, string? ContractType, string? Description, string Statut,
    string? TerminationReason, bool CustomerSigned, bool OfficerSigned
);

public record CreateContractRequest(
    string ClientID, int? ContractTypeID, int? CommissionTypeID, int? CommissionRangeID,
    string CollectionFrequency, string? CollectionDay,
    decimal? OpeningDeposit, decimal? MinimumBalance, decimal? MaximumBalance,
    string? PenaltyRules, int? GracePeriod,
    DateTime StartDate, DateTime? EndDate, string? ContractType, string? ContractDetails,
    string? Description, string? RenewalTerms, string? TerminationClause
);

public record UpdateContractRequest(
    string? CollectionFrequency, string? CollectionDay, DateTime? EndDate, string? Statut,
    int? CommissionTypeID, int? CommissionRangeID
);

public record TerminateContractRequest(string Reason); // Completed/CustomerRequest/Violation/Other
