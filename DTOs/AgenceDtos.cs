namespace DailySavingV.API.DTOs;

public record AgenceDto(
    int AgenceID, string CodeAgence, string Nom, string? Location, string? ContactInfo,
    string Statut, DateTime DateCreated, string? CreatedBy, string CodeIMF
);

public record CreateAgenceRequest(string CodeAgence, string Nom, string? Location, string? ContactInfo, string CodeIMF, int? VilleID);
