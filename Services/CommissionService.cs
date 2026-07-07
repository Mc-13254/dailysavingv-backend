using DailySavingV.API.Data;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

/// <summary>
/// Implements the automatic commission calculation described in the spec:
///   1. Determine the Transaction Type            -> parameter
///   2. Find the corresponding Commission Type     -> TransactionTypeToCommissionCode
///   3. Find the correct Commission Range           -> range whose Min/Max covers Montant
///   4. Apply the selected Calculation Method        -> Fixed or Percentage
///   5. Calculate the commission automatically       -> done here, no manual step
///   (Storage on the transaction record / receipt / reports is handled by
///    TransactionService, which calls this and persists HistCalculComis.)
/// </summary>
public class CommissionService : ICommissionService
{
    private readonly AppDbContext _db;

    // Maps each transaction type to the CommissionType.Code it should look up.
    private static readonly Dictionary<TransactionType, string> TransactionTypeToCommissionCode = new()
    {
        [TransactionType.DAILY_COLLECTION]  = "DAILY_SAVING",
        [TransactionType.DEPOSIT]           = "DEPOSIT",
        [TransactionType.WITHDRAWAL]        = "WITHDRAWAL",
        [TransactionType.LOAN_PAYMENT]      = "LOAN_PAYMENT",
        [TransactionType.ACCOUNT_OPENING]   = "ACCOUNT_OPENING",
        [TransactionType.ACCOUNT_CLOSING]   = "ACCOUNT_CLOSING",
        [TransactionType.TRANSFER]          = "MONEY_TRANSFER",
    };

    public CommissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CommissionResult> CalculateAsync(TransactionType transactionType, decimal montant)
    {
        if (!TransactionTypeToCommissionCode.TryGetValue(transactionType, out var code))
            throw new InvalidOperationException($"No Commission Type mapping defined for transaction type '{transactionType}'.");

        var commissionType = await _db.CommissionTypes
            .FirstOrDefaultAsync(ct => ct.Code == code && ct.Statut == "ACTIVE");

        if (commissionType == null)
            throw new InvalidOperationException($"No active Commission Type found for code '{code}'.");

        var range = await _db.CommissionRanges
            .Where(r => r.CommissionTypeID == commissionType.CommissionTypeID
                        && r.Statut == "ACTIVE"
                        && montant >= r.Inf
                        && montant <= r.Sup)
            .FirstOrDefaultAsync();

        if (range == null)
            throw new InvalidOperationException(
                $"No active Commission Range covers amount {montant} for Commission Type '{code}'. " +
                "An administrator must configure a range for this bracket.");

        decimal montantCommission;
        decimal appliedValue;

        if (range.CalculationMethod == CalculationMethod.FIXED)
        {
            montantCommission = range.Fixe!.Value;
            appliedValue = range.Fixe!.Value;
        }
        else // PERCENTAGE
        {
            appliedValue = range.TAUX!.Value;
            montantCommission = Math.Round(montant * range.TAUX!.Value / 100m, 2, MidpointRounding.AwayFromZero);
        }

        // Apply the floor/ceiling caps on the calculated commission, if configured.
        if (range.Minimum.HasValue && montantCommission < range.Minimum.Value)
            montantCommission = range.Minimum.Value;
        if (range.Maximum.HasValue && montantCommission > range.Maximum.Value)
            montantCommission = range.Maximum.Value;

        return new CommissionResult(
            commissionType.CommissionTypeID,
            range.CommissionRangeID,
            montantCommission,
            range.CalculationMethod.ToString(),
            appliedValue
        );
    }
}
