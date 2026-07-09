namespace DailySavingV.API.DTOs;

public record VaultDto(int VaultID, int AgenceID, string AgenceNom, decimal Balance, decimal? MinimumBalance, decimal? MaximumBalance);

public record CashMovementRowDto(
    int CashMovementID, string MovementNumber, string AgenceNom, string MovementType,
    string? FromCodeUser, string? FromUserName, string? ToCodeUser, string? ToUserName,
    decimal Amount, string? Reason, string Status, string RequestedBy, DateTime RequestDate,
    string? ApprovedBy, DateTime? ApprovalDate
);

public record CreateCashMovementRequest(string MovementType, string? FromCodeUser, string? ToCodeUser, decimal Amount, string? Reason);
public record RejectCashMovementRequest(string Reason);

public record TellerDashboardDto(
    decimal VaultBalance, int PendingMovements, decimal SuppliedToday, decimal ReturnedToday, decimal TransferredToday
);
