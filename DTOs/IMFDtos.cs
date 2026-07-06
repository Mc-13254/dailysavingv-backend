namespace DailySavingV.API.DTOs;

public record IMFDto(
    string CodeIMF, string Libelle, string? ShortName, string Statut,
    decimal TauxTaxe, bool AssujettiTaxe, string? SuffixeCompte, string? PrefixeCompte,
    int TailleCompte, bool CalculCommission,
    string? RegistrationNumber, string? TaxNumber, string? Description, string? LogoBase64,
    string? PrimaryPhone, string? SecondaryPhone, string? Email, string? Website,
    int? PaysID, string? PaysNom, int? VilleID, string? VilleNom, string? Address, string? PostalCode,
    string? CurrencyCode, string? Language, string? Timezone,
    string? CreatedBy, DateTime DateCreation, string? UpdatedBy, DateTime? UpdatedDate
);

public record CreateIMFRequest(
    string CodeIMF, string Libelle, string? ShortName,
    decimal TauxTaxe, bool AssujettiTaxe, string? SuffixeCompte, string? PrefixeCompte,
    int TailleCompte, bool CalculCommission,
    string? RegistrationNumber, string? TaxNumber, string? Description, string? LogoBase64,
    string? PrimaryPhone, string? SecondaryPhone, string? Email, string? Website,
    int? PaysID, int? VilleID, string? Address, string? PostalCode,
    string? CurrencyCode, string? Language, string? Timezone
);

// Same shape as create, minus the immutable fields (CodeIMF, RegistrationNumber
// are locked on the frontend and simply echoed back unused here).
public record UpdateIMFRequest(
    string Libelle, string? ShortName,
    decimal TauxTaxe, bool AssujettiTaxe, string? SuffixeCompte, string? PrefixeCompte,
    int TailleCompte, bool CalculCommission,
    string? TaxNumber, string? Description, string? LogoBase64,
    string? PrimaryPhone, string? SecondaryPhone, string? Email, string? Website,
    int? PaysID, int? VilleID, string? Address, string? PostalCode,
    string? CurrencyCode, string? Language, string? Timezone, string? Statut
);
