namespace DailySavingV.API.DTOs;

public record CommissionTypeDto(int CommissionTypeID, string Code, string Name, string? Description, string Statut);

public record CreateCommissionTypeRequest(string Code, string Name, string? Description);

public record CreateCommissionRangeRequest(
    int CommissionTypeID,
    string? Description,
    decimal Inf,
    decimal Sup,
    string CalculationMethod,   // "FIXED" or "PERCENTAGE"
    decimal? Fixe,
    decimal? TAUX,
    decimal? Minimum,
    decimal? Maximum,
    string CodeU
);

public record CommissionRangeDto(
    int CommissionRangeID, string? Description, int CommissionTypeID, string CommissionTypeName, string CodeComis,
    decimal Inf, decimal Sup, string CalculationMethod,
    decimal? Fixe, decimal? TAUX, decimal? Minimum, decimal? Maximum, string CodeU, string Statut,
    string? UserCreate, DateTime CreateDate, string? UserVal, DateTime? DateValidation,
    string? LastUserModif, DateTime? DateModification
);
