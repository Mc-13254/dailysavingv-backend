namespace DailySavingV.API.DTOs;

public record UserFullDto(
    string CodeUser, string Username, string? Email, string? Phone, string? Adresse,
    string? CNI, string? Photo, int RoleID, string RoleCode, string? RoleNom,
    string? FirstName, string? LastName, string? TypeUser,
    int? AgenceID, string? AgenceNom, string? CodeIMF,
    decimal? DebitMax, decimal? CreditMax, decimal? ValidationMax, decimal? PlafondCollect, decimal? Caution,
    string? Signe, string ValidationStatus, string Statut,
    string? CreatedBy, DateTime CreatedDate, DateTime? LastLogin,
    string? UserValidation, DateTime? DateValidation,
    string? LastUserModif, DateTime? DateModification,
    DateTime? LastDateSupervise, string? LastUserSupervise
);

public record CreateUserRequest(
    string Username, string Password, string ConfirmPassword, string? Email, string? Phone, string? SecondaryPhone,
    string? Adresse, string? CNI, int RoleID, string? TypeUser,
    string? FirstName, string? LastName, string? Photo, string? Signe,
    int? AgenceID, decimal? DebitMax, decimal? CreditMax, decimal? ValidationMax, decimal? PlafondCollect, decimal? Caution
);

public record UpdateUserRequest(
    string? Email, string? Phone, string? Adresse, string? CNI,
    int? RoleID, int? AgenceID, string? TypeUser, string? Statut, string? NewPassword,
    string? FirstName, string? LastName, string? Photo, string? Signe,
    decimal? DebitMax, decimal? CreditMax, decimal? ValidationMax, decimal? PlafondCollect, decimal? Caution
);
