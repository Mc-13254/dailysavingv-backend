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
    public int CommissionRangeID { get; set; } // RangeId
    public string? Description { get; set; }
    public int CommissionTypeID { get; set; }   // underlying FK (kept as int for referential integrity)
    public CommissionType? CommissionType { get; set; } // .Code exposed to the API as "CodeComis"

    public string CodeU { get; set; } = "XAF";  // currency code

    public decimal Inf { get; set; }  // lower bound of the transaction amount range
    public decimal Sup { get; set; }  // upper bound of the transaction amount range

    public CalculationMethod CalculationMethod { get; set; }
    public decimal? Fixe { get; set; } // fixed commission amount
    public decimal? TAUX { get; set; } // percentage rate

    public decimal? Minimum { get; set; } // floor cap applied to the calculated commission
    public decimal? Maximum { get; set; } // ceiling cap applied to the calculated commission

    public string Statut { get; set; } = "PENDING";   // ACTIVE / INACTIVE / PENDING

    public string? UserCreate { get; set; }
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public string? UserVal { get; set; }
    public DateTime? DateValidation { get; set; }
    public string? LastUserModif { get; set; }
    public DateTime? DateModification { get; set; }

    /// <summary>
    /// Server-side guard mirroring the DB CHECK constraints: exactly one of
    /// FixedAmount / PercentageRate must be set, consistent with CalculationMethod.
    /// </summary>
    public bool IsValid()
    {
        if (Inf >= Sup) return false;

        return CalculationMethod switch
        {
            CalculationMethod.FIXED => Fixe.HasValue && !TAUX.HasValue,
            CalculationMethod.PERCENTAGE => TAUX.HasValue && !Fixe.HasValue,
            _ => false
        };
    }
}
