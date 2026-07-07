namespace DailySavingV.API.DTOs;

public record PermissionDto(int PermissionID, string PermissionCode, string PermissionName, string Module, string Action);

public record RolePermissionStateDto(int PermissionID, string PermissionName, string Module, string Action, bool Allowed);

public record PermissionAllowedItem(int PermissionID, bool Allowed);

public record SaveRolePermissionsRequest(List<PermissionAllowedItem> Permissions);
