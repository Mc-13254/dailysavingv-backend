using DailySavingV.API.Entities;

namespace DailySavingV.API.Services.Interfaces;

public record CommissionResult(int CommissionTypeID, int CommissionRangeID, decimal MontantCommission, string CalculationMethod, decimal AppliedRateOrAmount);

public interface ICommissionService
{
    /// <summary>
    /// Given a transaction type and amount, finds the matching CommissionType
    /// and CommissionRange, applies the calculation method, and returns the
    /// computed commission. Throws if no active range covers the amount.
    /// </summary>
    Task<CommissionResult> CalculateAsync(TransactionType transactionType, decimal montant);
}
