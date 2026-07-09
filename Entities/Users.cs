namespace DailySavingV.API.Entities;

public class Users
{
    public string CodeUser { get; set; } = null!;   // e.g. U-001
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Adresse { get; set; }
    public string? CNI { get; set; }
    public string? Photo { get; set; }
    public int RoleID { get; set; }
    public Role? Role { get; set; }

    // Personal information
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? MaritalStatus { get; set; }
    public string? TypeUser { get; set; } // Administrator/Manager/Cashier/Collector/Supervisor/Auditor
    public string? Department { get; set; }
    public string? JobTitle { get; set; }

    // Location (personal address — distinct from the agency's own address)
    public int? PaysID { get; set; }
    public Pays? Pays { get; set; }
    public int? VilleID { get; set; }
    public Ville? Ville { get; set; }

    // Nullable only for HQ/Admin users who are not tied to a single agency
    public int? AgenceID { get; set; }
    public Agence? Agence { get; set; }
    public int? DepartmentID { get; set; }
    public Department? DepartmentRef { get; set; }

    // Financial settings
    public decimal? DebitMax { get; set; }
    public decimal? CreditMax { get; set; }
    public decimal? ValidationMax { get; set; }
    public decimal? PlafondCollect { get; set; }
    public decimal? Caution { get; set; }

    // Attachments
    public string? Signe { get; set; } // signature (base64)

    public string Statut { get; set; } = "ACTIVE";

    // Security / account lockout
    public int FailedLoginAttempts { get; set; }
    public bool AccountLocked { get; set; }
    public string? LockReason { get; set; }
    public DateTime? LockedDate { get; set; }
    public string? LockedBy { get; set; }
    public string ValidationStatus { get; set; } = "VALIDATED";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangedDate { get; set; }

    // Audit trail
    public string? UserValidation { get; set; }
    public DateTime? DateValidation { get; set; }
    public string? LastUserModif { get; set; }
    public DateTime? DateModification { get; set; }
    public DateTime? LastDateSupervise { get; set; }
    public string? LastUserSupervise { get; set; }
}

public class FailedLoginAttempt
{
    public int AttemptID { get; set; }
    public string Username { get; set; } = null!;
    public string? CodeUser { get; set; } // filled in when the username actually matched a user
    public string FailureReason { get; set; } = null!; // WRONG_PASSWORD / UNKNOWN_USERNAME / LOCKED_ACCOUNT / INACTIVE_ACCOUNT
    public string RiskLevel { get; set; } = "LOW"; // LOW/MEDIUM/HIGH/CRITICAL, based on recent attempt frequency
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime AttemptDate { get; set; } = DateTime.UtcNow;
}

// ---- Password Policy (single global policy — simplification: not per-role) ----
public class PasswordPolicy
{
    public int PasswordPolicyID { get; set; }
    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 64;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireNumber { get; set; } = true;
    public bool RequireSpecialCharacter { get; set; } = true;
    public int PasswordExpirationDays { get; set; } = 90; // 0 = never expires
    public int PasswordHistoryCount { get; set; } = 5;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class PasswordHistory
{
    public int PasswordHistoryID { get; set; }
    public string CodeUser { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    public string? ChangedBy { get; set; }
}

// ---- API Management (simplified: key issuance/revocation, no enforcement middleware yet) ----
public class ApiKey
{
    public int ApiKeyID { get; set; }
    public string Name { get; set; } = null!;
    public string KeyHash { get; set; } = null!;     // never store the raw key
    public string KeyPrefix { get; set; } = null!;   // first few chars shown in the UI for identification
    public string? Description { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? RevokedBy { get; set; }
    public DateTime? RevokedDate { get; set; }
}

// ---- Error Logs ----
public class ErrorLog
{
    public long ErrorLogID { get; set; }
    public string Message { get; set; } = null!;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? RequestMethod { get; set; }
    public string? CodeUser { get; set; }
    public string? IPAddress { get; set; }
    public DateTime OccurredDate { get; set; } = DateTime.UtcNow;
}

public class RefreshToken
{
    public int TokenID { get; set; }
    public string CodeUser { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiryDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? TerminationReason { get; set; }
    public string? TerminatedBy { get; set; }
}
