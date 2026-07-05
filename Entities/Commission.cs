namespace DailySavingV.API.Entities;

public enum CalculationMethod
{
    FIXED,
    PERCENTAGE
}

public class CommissionType
{
    public int CommissionTypeID { get; set; }
    public string Code { get; set; } = null!;   // DAILY_SAVING / DEPOSIT / WITHDRAWAL ...
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Statut { get; set; } = "ACTIVE";
    public string ValidationStatus { get; set; } = "VALIDATED";
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<CommissionRange> Ranges { get; set; } = new List<CommissionRange>();
}

public class CommissionRange
{
    public int CommissionRangeID { get; set; }
    public int CommissionTypeID { get; set; }
    public CommissionType? CommissionType { get; set; }

    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }

    public CalculationMethod CalculationMethod { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? PercentageRate { get; set; }

    public string Currency { get; set; } = "XAF";
    public string Statut { get; set; } = "PENDING";   // ACTIVE / INACTIVE / PENDING

    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? ValidatedBy { get; set; }
    public DateTime? ValidationDate { get; set; }

    /// <summary>
    /// Server-side guard mirroring the DB CHECK constraints: exactly one of
    /// FixedAmount / PercentageRate must be set, consistent with CalculationMethod.
    /// </summary>
    public bool IsValid()
    {
        if (MinAmount >= MaxAmount) return false;

        return CalculationMethod switch
        {
            CalculationMethod.FIXED => FixedAmount.HasValue && !PercentageRate.HasValue,
            CalculationMethod.PERCENTAGE => PercentageRate.HasValue && !FixedAmount.HasValue,
            _ => false
        };
    }
}
