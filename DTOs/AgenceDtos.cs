namespace DailySavingV.API.DTOs;

public record AgenceDto(
    int AgenceID, string CodeAgence, string Nom, string? ShortName, string? Description, string? LogoBase64,
    string? PrimaryPhone, string? SecondaryPhone, string? Email, string? Website,
    int? PaysID, string? PaysNom, int? VilleID, string? VilleNom, string? Address, string? PostalCode,
    string CodeIMF, string? IMFNom, string? ManagerId, string? ManagerNom, DateTime? OpeningDate,
    string Statut, DateTime DateCreated, string? CreatedBy, string? UpdatedBy, DateTime? UpdatedDate
);

public record CreateAgenceRequest(
    string CodeAgence, string Nom, string? ShortName, string? Description, string? LogoBase64,
    string? PrimaryPhone, string? SecondaryPhone, string? Email, string? Website,
    int? PaysID, int? VilleID, string? Address, string? PostalCode,
    string CodeIMF, string? ManagerId, DateTime? OpeningDate
);

public record UpdateAgenceRequest(
    string Nom, string? ShortName, string? Description, string? LogoBase64,
    string? PrimaryPhone, string? SecondaryPhone, string? Email, string? Website,
    int? PaysID, int? VilleID, string? Address, string? PostalCode,
    string? ManagerId, DateTime? OpeningDate, string? Statut
);
