namespace DailySavingV.API.DTOs;

public record IMFDto(
    string CodeIMF, string Libelle, string Statut, decimal TauxTaxe, bool AssujettiTaxe,
    string? SuffixeCompte, string? PrefixeCompte, int TailleCompte, bool CalculCommission, DateTime DateCreation
);

public record CreateIMFRequest(
    string CodeIMF, string Libelle, decimal TauxTaxe, bool AssujettiTaxe,
    string? SuffixeCompte, string? PrefixeCompte, int TailleCompte, bool CalculCommission
);
