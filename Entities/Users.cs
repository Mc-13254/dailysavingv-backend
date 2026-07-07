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
    public string ValidationStatus { get; set; } = "VALIDATED";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    // Audit trail
    public string? UserValidation { get; set; }
    public DateTime? DateValidation { get; set; }
    public string? LastUserModif { get; set; }
    public DateTime? DateModification { get; set; }
    public DateTime? LastDateSupervise { get; set; }
    public string? LastUserSupervise { get; set; }
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
}
