namespace DailySavingV.API.DTOs;

public record ClientDto(
    string ClientID, string Nom, string? Prenom, string? PhoneNumber, string? Email,
    string ClientType, int AgenceID, string ValidationStatus
);

public record CreateClientRequest(
    string Nom, string? Prenom, string? Sexe, string? PhoneNumber, string? Address,
    string? Email, string? CompanyName, string ClientType, int? TypeCNIID,
    string? NumeroCNI, string? CollectorID
);
