namespace DailySavingV.API.Entities;

/// <summary>
/// Transparent, rule-based fraud risk scoring — NOT a trained ML model. Every
/// point on the 0-100 score comes from a named, explainable business rule
/// (see FraudDetectionService). This is deliberate: a real banking system
/// needs auditors to be able to say exactly why a transaction was flagged,
/// which a black-box model can't guarantee without much more infrastructure.
/// </summary>
public class FraudDetection
{
    public int FraudDetectionID { get; set; }
    public long TransactionID { get; set; }
    public int Score { get; set; }              // 0-100
    public string RiskLevel { get; set; } = "LOW"; // LOW / MEDIUM / HIGH / CRITICAL
    public string FactorsJson { get; set; } = null!; // [{rule, weight, description}]
    public bool FlaggedForReview { get; set; }
    public string ReviewStatus { get; set; } = "NONE"; // NONE / PENDING / CLEARED / CONFIRMED_FRAUD
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
