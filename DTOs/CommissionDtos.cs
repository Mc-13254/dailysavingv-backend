namespace DailySavingV.API.DTOs;

public record CommissionTypeDto(int CommissionTypeID, string Code, string Name, string? Description, string Statut);

public record CreateCommissionRangeRequest(
    int CommissionTypeID,
    decimal MinAmount,
    decimal MaxAmount,
    string CalculationMethod,   // "FIXED" or "PERCENTAGE"
    decimal? FixedAmount,
    decimal? PercentageRate,
    string Currency
);

public record CommissionRangeDto(
    int CommissionRangeID, int CommissionTypeID, string CommissionTypeName,
    decimal MinAmount, decimal MaxAmount, string CalculationMethod,
    decimal? FixedAmount, decimal? PercentageRate, string Currency, string Statut
);
