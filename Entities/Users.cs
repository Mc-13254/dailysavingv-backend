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

    // Nullable only for HQ/Admin users who are not tied to a single agency
    public int? AgenceID { get; set; }
    public Agence? Agence { get; set; }

    public string Statut { get; set; } = "ACTIVE";
    public string ValidationStatus { get; set; } = "VALIDATED";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
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
