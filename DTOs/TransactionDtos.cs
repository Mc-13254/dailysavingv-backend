using DailySavingV.API.Entities;

namespace DailySavingV.API.DTOs;

public record CreateTransactionRequest(
    TransactionType TransactionType,
    string AccountID,
    string? ToAccountID,      // required for TRANSFER
    string? CollectorID,
    decimal Montant,
    string? RemitterName,
    string? BeneficiaryName,
    string? PaymentMethod,
    Dictionary<int, int>? CashBreakdown  // denomination value -> quantity, CASH only
);

public record TransactionReceiptDto(
    long TransactionID,
    string ReceiptNumber,
    string TransactionType,
    string AccountID,
    string? AccountNumber,
    string? ToAccountID,
    string ClientID,
    string? ClientName,
    string? CollectorID,
    string? CollectorName,
    string? RemitterName,
    string? BeneficiaryName,
    decimal Montant,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal MontantCommission,
    DateTime DateTransaction
);

// ---- Client account lookup for the transaction form ----
public record ClientAccountLookupDto(
    string ClientID, string ClientName, string? PhoneNumber, string? CollectorID, string? CollectorName,
    List<AccountLookupDto> Accounts
);
public record AccountLookupDto(string AccountID, string AccountType, decimal Balance, string Status, int? ContractID, string? ContractNumber);

// ---- Excel bulk import (goes through Maker-Checker like everything else) ----
public record ImportTransactionRowRequest(
    string TransactionType, string AccountID, string? ToAccountID, string? CollectorID,
    decimal Montant, string? RemitterName, string? BeneficiaryName, string? RefRowLabel
);
public record CreateImportBatchRequest(string FileName, List<ImportTransactionRowRequest> Rows);

public record ImportBatchRowDto(
    int RowID, int RowNumber, string TransactionType, string AccountID, string? ToAccountID,
    string? CollectorID, decimal Montant, string? RemitterName, string? BeneficiaryName,
    string Status, string? ErrorMessage, string? RefRowLabel
);
public record ImportBatchDto(int BatchID, string FileName, string UploadedBy, DateTime UploadedDate, int TotalRows, string Status, List<ImportBatchRowDto> Rows);

