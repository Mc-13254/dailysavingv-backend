using DailySavingV.API.Entities;

namespace DailySavingV.API.DTOs;

public record CreateTransactionRequest(
    TransactionType TransactionType,
    string AccountID,
    string? CollectorID,
    decimal Montant
);

public record TransactionReceiptDto(
    long TransactionID,
    string ReceiptNumber,
    string TransactionType,
    decimal Montant,
    decimal MontantCommission,
    DateTime DateTransaction
);
