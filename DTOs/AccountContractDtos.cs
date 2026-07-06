namespace DailySavingV.API.DTOs;

public record AccountFullDto(
    string AccountID, string ClientID, string? NumCarnet, decimal Balance, bool Active,
    int AgenceID, string? CreatedBy, DateTime CreateDate
);

public record CreateAccountRequest(string ClientID, string? NumCarnet);

public record ContractFullDto(
    int ContractID, string ContractNumber, string? ClientID, int? AgenceID,
    DateTime StartDate, DateTime? EndDate, string? ContractType, string? Description, string Statut
);

public record CreateContractRequest(
    string ContractNumber, string? ClientID, DateTime StartDate, DateTime? EndDate,
    string? ContractType, string? ContractDetails, string? Description,
    string? RenewalTerms, string? TerminationClause
);
