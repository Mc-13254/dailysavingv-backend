namespace DailySavingV.API.DTOs;

public record ContractTypeDto(
    int ContractTypeID, string ContractCode, string ContractName, string? ShortName, string? Description,
    bool AllowDailyCollection, bool AllowWeeklyCollection, bool AllowMonthlyCollection,
    decimal? MinimumCollectionAmount, decimal? MaximumCollectionAmount, decimal? DefaultCollectionAmount,
    decimal? MinimumOpeningBalance, decimal? MaximumBalance, decimal? InterestRate,
    int? ContractDuration, string? DurationUnit, decimal? PenaltyAmount, int? GracePeriod,
    string Statut, string? CreatedBy, DateTime CreatedDate, string? UpdatedBy, DateTime? UpdatedDate
);

public record CreateContractTypeRequest(
    string ContractName, string? ShortName, string? Description,
    bool AllowDailyCollection, bool AllowWeeklyCollection, bool AllowMonthlyCollection,
    decimal? MinimumCollectionAmount, decimal? MaximumCollectionAmount, decimal? DefaultCollectionAmount,
    decimal? MinimumOpeningBalance, decimal? MaximumBalance, decimal? InterestRate,
    int? ContractDuration, string? DurationUnit, decimal? PenaltyAmount, int? GracePeriod
);

public record UpdateContractTypeRequest(
    string ContractName, string? ShortName, string? Description,
    bool AllowDailyCollection, bool AllowWeeklyCollection, bool AllowMonthlyCollection,
    decimal? MinimumCollectionAmount, decimal? MaximumCollectionAmount, decimal? DefaultCollectionAmount,
    decimal? MinimumOpeningBalance, decimal? MaximumBalance, decimal? InterestRate,
    int? ContractDuration, string? DurationUnit, decimal? PenaltyAmount, int? GracePeriod, string Statut
);
