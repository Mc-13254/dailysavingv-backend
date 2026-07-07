namespace DailySavingV.API.DTOs;

public record RoleDto(
    int RoleID, string Code, string Libelle, string? Description, bool Statut,
    int UsersCount, string? CreatedBy, DateTime CreatedDate, string? UpdatedBy, DateTime? UpdatedDate
);

public record CreateRoleRequest(string Libelle, string? Description);

public record UpdateRoleRequest(string Libelle, string? Description, bool Statut);
