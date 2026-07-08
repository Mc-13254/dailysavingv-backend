namespace DailySavingV.API.DTOs;

public record BusinessCalendarDto(int AgenceID, string WorkingDays, TimeSpan OpeningTime, TimeSpan ClosingTime, int GracePeriodMinutes);
public record UpdateBusinessCalendarRequest(string WorkingDays, TimeSpan OpeningTime, TimeSpan ClosingTime, int GracePeriodMinutes);

public record OpenSessionRequest(decimal? OpeningCashOverride, string? Comment);
public record CloseSessionRequest(decimal PhysicalCash, string? Comment, string? VarianceReason);

public record CashSessionDto(
    int CashSessionID, string SessionNumber, string CodeUser, string? UserFullName, int AgenceID,
    DateTime OpeningDate, decimal OpeningCash, decimal PreviousClosingCash,
    DateTime? ClosingDate, decimal? ExpectedCash, decimal? PhysicalCash, decimal? CashDifference,
    string Status, bool RequiresApproval, string? ApprovalStatus
);

public record SessionDashboardDto(
    decimal OpeningCash, decimal CurrentCash, decimal ExpectedCash,
    decimal Collections, decimal Deposits, decimal Withdrawals, decimal Transfers,
    int CollectionsCount, int DepositsCount, int WithdrawalsCount, int TransfersCount,
    int PendingOperations, int ApprovedOperations, int RejectedOperations,
    decimal CommissionGenerated, decimal CashDifference, string Status
);

public record SessionHistoryFilter(DateTime? From, DateTime? To, string? CodeUser, int? AgenceID, string? Status);
