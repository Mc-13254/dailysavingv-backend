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

// ---- Password Policy ----

public record PasswordPolicyDto(
    int MinimumLength, int MaximumLength, bool RequireUppercase, bool RequireLowercase,
    bool RequireNumber, bool RequireSpecialCharacter, int PasswordExpirationDays, int PasswordHistoryCount
);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

// ---- API Management ----

public record ApiKeyRowDto(
    int ApiKeyID, string Name, string KeyPrefix, string? Description,
    DateTime? ExpiryDate, bool IsActive, DateTime? LastUsedDate, string CreatedBy, DateTime CreatedDate
);

public record CreateApiKeyRequest(string Name, string? Description, DateTime? ExpiryDate);
public record CreateApiKeyResponseDto(int ApiKeyID, string FullKey); // FullKey shown once, never again

// ---- Error Logs ----

public record ErrorLogRowDto(
    long ErrorLogID, string Message, string? ExceptionType, string? RequestPath,
    string? RequestMethod, string? CodeUser, DateTime OccurredDate
);

// ---- System Health ----

public record SystemHealthDto(
    string ApiStatus, string DatabaseStatus, DateTime ServerTimeUtc, TimeSpan Uptime,
    int ErrorsLast24h, int ActiveSessions
);
