namespace DailySavingV.API.Entities;

public class Role
{
    public int RoleID { get; set; }
    public string Code { get; set; } = null!;   // ADMIN / SUPERVISOR / COLLECTOR
    public string Libelle { get; set; } = null!;
    public string? Description { get; set; }
    public bool Statut { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class Fonctionnalite
{
    public int FonctionnaliteID { get; set; }
    public string Code { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public string? Module { get; set; }
    public int? ParentID { get; set; }
    public bool Statut { get; set; } = true;
}

public class Habilitation
{
    public int HabilitationID { get; set; }
    public string Code { get; set; } = null!;   // CREATE / READ / UPDATE / DELETE / VALIDATE / EXPORT
    public string Libelle { get; set; } = null!;
}

public class Habiliter
{
    public int RoleID { get; set; }
    public int FonctionnaliteID { get; set; }
    public int HabilitationID { get; set; }
}

public class RoleFonctionnalite
{
    public int RoleID { get; set; }
    public int FonctionnaliteID { get; set; }
}

public class Permission
{
    public int PermissionID { get; set; }
    public string PermissionCode { get; set; } = null!;
    public string PermissionName { get; set; } = null!; // e.g. "View", "Create", "Approve"
    public string Module { get; set; } = null!;         // e.g. "IMF", "Collectors", "Operations"
    public string Action { get; set; } = null!;          // e.g. "VIEW", "CREATE", "EDIT", "DELETE", "APPROVE"
    public string? Description { get; set; }
}

public class RolePermission
{
    public int RolePermissionID { get; set; }
    public int RoleID { get; set; }
    public Role? Role { get; set; }
    public int PermissionID { get; set; }
    public Permission? Permission { get; set; }
    public bool Allowed { get; set; }
}
