namespace DailySavingV.API.DTOs;

public record DepartmentDto(
    int DepartmentID, string DepartmentCode, string DepartmentName, string? ShortName, string? Description,
    string CodeIMF, string? IMFNom, int AgenceID, string? AgenceNom,
    string? ManagerId, string? ManagerNom, string Statut,
    string? CreatedBy, DateTime CreatedDate, string? UpdatedBy, DateTime? UpdatedDate
);

public record CreateDepartmentRequest(
    string DepartmentName, string? ShortName, string? Description,
    string CodeIMF, int AgenceID, string? ManagerId
);

public record UpdateDepartmentRequest(
    string DepartmentName, string? ShortName, string? Description,
    int AgenceID, string? ManagerId, string Statut
);
