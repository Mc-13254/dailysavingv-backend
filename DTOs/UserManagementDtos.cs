namespace DailySavingV.API.DTOs;

public record UserFullDto(
    string CodeUser, string Username, string? Email, string? Phone, string? Adresse,
    string? CNI, string RoleCode, int? AgenceID, string ValidationStatus, string Statut
);

public record CreateUserRequest(
    string Username, string Password, string? Email, string? Phone,
    string? Adresse, string? CNI, int RoleID
);
