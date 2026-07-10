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
    /// Single entry point for every financial transaction (Daily Collection,
    /// Deposit, Withdrawal, Transfer). Requires an OPEN Cash Session, resolves
    /// the client/account "bank receipt" style details (account, collector,
    /// remitter, beneficiary), calculates commission automatically, updates
    /// balance(s), and returns everything needed to print a receipt.
    /// </summary>
    public async Task<TransactionReceiptDto> CreateAndValidateAsync(CreateTransactionRequest request, string validatedByCodeUser)
    {
        var session = await _db.CashSessions
            .FirstOrDefaultAsync(s => s.CodeUser == validatedByCodeUser && s.Status == "OPEN")
            ?? throw new InvalidOperationException("Vous devez ouvrir votre session de caisse avant d'effectuer une transaction.");

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == request.AccountID)
            ?? throw new InvalidOperationException("Compte introuvable.");

        if (request.Montant <= 0)
            throw new InvalidOperationException("Le montant doit être supérieur à zéro.");

        // Real banking cash handling: when counted in cash, the bill/coin
        // breakdown must add up exactly to the declared amount.
        if (request.CashBreakdown is { Count: > 0 })
        {
            var breakdownTotal = request.CashBreakdown.Sum(kv => (decimal)kv.Key * kv.Value);
            if (breakdownTotal != request.Montant)
                throw new InvalidOperationException(
                    $"Le détail des coupures ({breakdownTotal:N0}) ne correspond pas au montant de la transaction ({request.Montant:N0}).");
        }

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == account.ClientID)
            ?? throw new InvalidOperationException("Client introuvable.");
        var clientName = $"{client.Nom} {client.Prenom}".Trim();

        await ValidatePartyIsActiveAsync(client, account, isSender: true);

        Accounts? toAccount = null;
        Client? toClient = null;
        if (request.TransactionType == TransactionType.TRANSFER)
        {
            if (string.IsNullOrWhiteSpace(request.ToAccountID))
                throw new InvalidOperationException("Le compte bénéficiaire est requis pour un transfert.");
            if (request.ToAccountID == request.AccountID)
                throw new InvalidOperationException("Le compte source et le compte bénéficiaire doivent être différents.");

            toAccount = await _db.Accounts.FirstOrDefaultAsync(a => a.AccountID == request.ToAccountID)
                ?? throw new InvalidOperationException("Compte bénéficiaire introuvable.");
            toClient = await _db.Clients.FirstOrDefaultAsync(c => c.ClientID == toAccount.ClientID)
                ?? throw new InvalidOperationException("Client bénéficiaire introuvable.");

            await ValidatePartyIsActiveAsync(toClient, toAccount, isSender: false);

            if (account.Balance < request.Montant)
                throw new InvalidOperationException("Solde insuffisant sur le compte source.");
            if (account.MinimumBalance.HasValue && account.Balance - request.Montant < account.MinimumBalance.Value)
                throw new InvalidOperationException("Cette opération ferait descendre le compte source sous son solde minimum autorisé.");
        }
        else if (request.TransactionType is TransactionType.WITHDRAWAL or TransactionType.LOAN_PAYMENT)
        {
            if (account.Balance < request.Montant)
                throw new InvalidOperationException("Solde insuffisant.");
            if (account.MinimumBalance.HasValue && account.Balance - request.Montant < account.MinimumBalance.Value)
                throw new InvalidOperationException("Cette opération ferait descendre le compte sous son solde minimum autorisé.");
        }

        var openingBalance = account.Balance;
        var commission = await _commissionService.CalculateAsync(request.TransactionType, request.Montant);

        // Bank-receipt fields: default Remitter/Beneficiary to the actual client
        // name(s) if the teller didn't override them (e.g. someone deposits on
        // behalf of the account holder, or cash handed to a different person).
        var remitter = request.RemitterName;
        var beneficiary = request.BeneficiaryName;
        if (request.TransactionType == TransactionType.TRANSFER)
        {
            remitter ??= clientName;
            beneficiary ??= toClient != null ? $"{toClient.Nom} {toClient.Prenom}".Trim() : toAccount?.ClientID;
        }
        else if (request.TransactionType is TransactionType.DEPOSIT or TransactionType.DAILY_COLLECTION)
        {
            remitter ??= clientName;
            beneficiary ??= clientName; // money is for the account holder themselves
        }
        else // WITHDRAWAL / LOAN_PAYMENT
        {
            remitter ??= clientName;   // funds come out of the client's account
            beneficiary ??= clientName; // paid out to the client themselves by default
        }

        var transaction = new Transactions
        {
            TransactionType = request.TransactionType,
            AccountID = account.AccountID,
            ToAccountID = toAccount?.AccountID,
            ToClientID = toClient?.ClientID,
            ClientID = account.ClientID,
            CollectorID = request.CollectorID,
            CashSessionID = session.CashSessionID,
            AgenceID = _currentUser.AgenceID ?? account.AgenceID,
            Montant = request.Montant,
            OpeningBalance = openingBalance,
            RemitterName = remitter,
            BeneficiaryName = beneficiary,
            PaymentMethod = request.PaymentMethod,
            CashBreakdownJson = request.CashBreakdown is { Count: > 0 }
                ? JsonSerializer.Serialize(request.CashBreakdown)
                : null,
            CommissionTypeID = commission.CommissionTypeID,
            CommissionRangeID = commission.CommissionRangeID,
            MontantCommission = commission.MontantCommission,
            ReceiptNumber = GenerateReceiptNumber(),
            Statut = "VALIDATED",
            CreatedBy = _currentUser.CodeUser,
            ValidatedBy = validatedByCodeUser,
            ValidationDate = DateTime.UtcNow
        };

        _db.Transactions.Add(transaction);

        switch (request.TransactionType)
        {
            case TransactionType.DEPOSIT:
            case TransactionType.DAILY_COLLECTION:
                account.Balance += request.Montant;
                account.AvailableBalance += request.Montant;
                break;
            case TransactionType.WITHDRAWAL:
            case TransactionType.LOAN_PAYMENT:
                account.Balance -= request.Montant;
                account.AvailableBalance -= request.Montant;
                break;
            case TransactionType.TRANSFER:
                account.Balance -= request.Montant;
                account.AvailableBalance -= request.Montant;
                toAccount!.Balance += request.Montant;
                toAccount.AvailableBalance += request.Montant;
                break;
        }

        transaction.ClosingBalance = account.Balance;

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

        string? collectorName = null;
        if (!string.IsNullOrWhiteSpace(request.CollectorID))
        {
            var collector = await _db.Collectors.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.CollectorID == request.CollectorID);
            if (collector != null) collectorName = $"{collector.Name} {collector.Surname}".Trim();
        }

        return new TransactionReceiptDto(
            transaction.TransactionID, transaction.ReceiptNumber!, transaction.TransactionType.ToString(),
            transaction.AccountID, account.NumCarnet, transaction.ToAccountID,
            transaction.ClientID, clientName, transaction.CollectorID, collectorName,
            transaction.RemitterName, transaction.BeneficiaryName,
            transaction.Montant, openingBalance, account.Balance, transaction.MontantCommission,
            transaction.DateTransaction
        );
    }

    private async Task ValidatePartyIsActiveAsync(Client client, Accounts account, bool isSender)
    {
        var role = isSender ? "émetteur" : "bénéficiaire";

        if (client.ValidationStatus != "VALIDATED")
            throw new InvalidOperationException($"Le client {role} n'est pas actif (statut: {client.ValidationStatus}).");
        if (client.IsBlacklisted)
            throw new InvalidOperationException($"Le client {role} est sur liste noire — opération bloquée.");
        if (account.Status != "ACTIVE")
            throw new InvalidOperationException($"Le compte {role} n'est pas actif (statut: {account.Status}).");

        if (account.ContractID.HasValue)
        {
            var contract = await _db.Contracts.FirstOrDefaultAsync(c => c.ContractID == account.ContractID.Value);
            if (contract != null && contract.Statut != "ACTIVE")
                throw new InvalidOperationException($"Le contrat lié au compte {role} n'est pas actif (statut: {contract.Statut}).");
        }
    }

    private static string GenerateReceiptNumber() =>
        $"RCT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
}
