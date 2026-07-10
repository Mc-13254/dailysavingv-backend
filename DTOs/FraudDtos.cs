namespace DailySavingV.API.DTOs;

public record FraudRowDto(
    int FraudDetectionID, long TransactionID, string? ReceiptNumber, string TransactionType,
    string ClientName, string? CollectorName, string AgenceNom, decimal Montant,
    int Score, string RiskLevel, bool FlaggedForReview, string ReviewStatus, DateTime CreatedDate
);

public record FraudFactorDto(string Rule, int Weight, string Description);

public record FraudDetailDto(
    int FraudDetectionID, long TransactionID, string? ReceiptNumber, string ClientName, string? CollectorName,
    string AgenceNom, decimal Montant, DateTime DateTransaction,
    int Score, string RiskLevel, List<FraudFactorDto> Factors,
    bool FlaggedForReview, string ReviewStatus, string? ReviewedBy, DateTime? ReviewDate, string? ReviewComment
);

public record ReviewFraudRequest(string ReviewStatus, string? Comment); // CLEARED / CONFIRMED_FRAUD

public record FraudStatsDto(
    int TodayFlagged, int PendingReview, int TotalCritical, int TotalHigh,
    double AverageScore, int ConfirmedFraudCount, int ClearedCount
);
