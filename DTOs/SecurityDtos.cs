namespace DailySavingV.API.DTOs;

// ---- Failed Login Attempts ----

public record FailedLoginRowDto(
    int AttemptID, string Username, string? CodeUser, string? FullName,
    string FailureReason, string RiskLevel, string? IPAddress, string? UserAgent, DateTime AttemptDate
);

public record FailedLoginStatsDto(
    int Today, int ThisWeek, int ThisMonth,
    int LockedAccounts, int HighRiskAttempts
);

// ---- Account Lockout ----

public record LockedAccountRowDto(
    string CodeUser, string Username, string? FullName, string? RoleCode, string? AgenceNom,
    int FailedLoginAttempts, string? LockReason, DateTime? LockedDate, string? LockedBy
);

public record LockAccountRequest(string Reason);

// ---- Active Sessions ----

public record ActiveSessionRowDto(
    int TokenID, string CodeUser, string Username, string? FullName, string? RoleCode, string? AgenceNom,
    string? IPAddress, string? UserAgent, DateTime CreatedDate, DateTime ExpiryDate, bool IsActive
);

public record SessionStatsDto(
    int TotalActiveSessions, int CollectorsOnline, int CashiersOnline, int ManagersOnline, int AdministratorsOnline
);

public record TerminateSessionRequest(string Reason);
