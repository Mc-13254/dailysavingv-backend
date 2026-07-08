using System.Text.Json;
using DailySavingV.API.Data;
using DailySavingV.API.DTOs;
using DailySavingV.API.Entities;
using DailySavingV.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DailySavingV.API.Services;

public interface ITransactionService
{
    Task<TransactionReceiptDto> CreateAndValidateAsync(CreateTransactionRequest request, string validatedByCodeUser);
}

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    private readonly ICommissionService _commissionService;
    private readonly ICurrentUserService _currentUser;

    public TransactionService(AppDbContext db, ICommissionService commissionService, ICurrentUserService currentUser)
    {
        _db = db;
        _commissionService = commissionService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Requirement: "Whenever a Deposit, Withdrawal, Daily Collection, or any
    /// other transaction is validated, the system must automatically calculate
    /// and store the commission - no manual calculation required."
    /// This method is the single entry point for that rule; every transaction
    /// endpoint funnels through here so the behavior can never be skipped.
    /// </summary>
    public async Task<TransactionReceiptDto> CreateAndValidateAsync(CreateTransactionRequest request, string validatedByCodeUser)
    {
        // Business rule (Cash Session module): no financial transaction can be
        // performed before the user has opened their working day.
        var session = await _db.CashSessions
            .FirstOrDefaultAsync(s => s.CodeUser == validatedByCodeUser && s.Status == "OPEN")
            ?? throw new InvalidOperationException("Vous devez ouvrir votre session de caisse avant d'effectuer une transaction.");

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == request.AccountID)
            ?? throw new InvalidOperationException("Account not found or not in your agency.");

        // 1 & 2 & 3 & 4 & 5: delegate the actual calculation to the commission engine
        var commission = await _commissionService.CalculateAsync(request.TransactionType, request.Montant);

        var transaction = new Transactions
        {
            TransactionType = request.TransactionType,
            AccountID = account.AccountID,
            ClientID = account.ClientID,
            CollectorID = request.CollectorID,
            CashSessionID = session.CashSessionID,
            AgenceID = _currentUser.AgenceID ?? account.AgenceID,
            Montant = request.Montant,
            CommissionTypeID = commission.CommissionTypeID,
            CommissionRangeID = commission.CommissionRangeID,
            MontantCommission = commission.MontantCommission,      // 6. Store the commission in the transaction record
            ReceiptNumber = GenerateReceiptNumber(),
            Statut = "VALIDATED",
            CreatedBy = _currentUser.CodeUser,
            ValidatedBy = validatedByCodeUser,
            ValidationDate = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);

        // Apply balance movement
        switch (request.TransactionType)
        {
            case TransactionType.DEPOSIT:
            case TransactionType.DAILY_COLLECTION:
                account.Balance += request.Montant;
                break;
            case TransactionType.WITHDRAWAL:
            case TransactionType.LOAN_PAYMENT:
                if (account.Balance < request.Montant)
                    throw new InvalidOperationException("Insufficient balance.");
                account.Balance -= request.Montant;
                break;
        }

        await _db.SaveChangesAsync(); // need TransactionID before writing audit rows

        _db.HistCalculComis.Add(new HistCalculComis
        {
            TransactionID = transaction.TransactionID,
            CommissionTypeID = commission.CommissionTypeID,
            CommissionRangeID = commission.CommissionRangeID,
            MontantTransaction = request.Montant,
            CalculationMethod = commission.CalculationMethod,
            TauxAppliqueOuFixe = commission.AppliedRateOrAmount,
            MontantCommission = commission.MontantCommission
        });

        _db.HistTransactions.Add(new HistTransactions
        {
            TransactionID = transaction.TransactionID,
            Action = "VALIDATE",
            NewData = JsonSerializer.Serialize(transaction),
            ActionBy = validatedByCodeUser
        });

        await _db.SaveChangesAsync();

        // 7 & 8: receipt + reporting data - reports/dashboards simply query
        // Transactions.MontantCommission, so nothing further is needed there.
        return new TransactionReceiptDto(
            transaction.TransactionID,
            transaction.ReceiptNumber!,
            transaction.TransactionType.ToString(),
            transaction.Montant,
            transaction.MontantCommission,
            transaction.DateTransaction
        );
    }

    private static string GenerateReceiptNumber() =>
        $"RCT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
}
