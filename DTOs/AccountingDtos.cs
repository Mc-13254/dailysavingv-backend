namespace DailySavingV.API.DTOs;

public record GLAccountDto(int GLAccountID, string Code, string Name, string Type, string NormalBalance, bool IsCashAccount);

public record TrialBalanceRowDto(string Code, string Name, string Type, decimal TotalDebit, decimal TotalCredit, decimal Balance);
public record TrialBalanceDto(List<TrialBalanceRowDto> Rows, decimal TotalDebit, decimal TotalCredit, bool IsBalanced);

public record GeneralLedgerLineDto(
    int JournalEntryID, DateTime EntryDate, string EntryNumber, string Description, string SourceType, string? SourceReference,
    decimal Debit, decimal Credit, decimal RunningBalance
);
public record GeneralLedgerDto(string AccountCode, string AccountName, decimal OpeningBalance, List<GeneralLedgerLineDto> Lines, decimal ClosingBalance);

public record BalanceSheetSectionDto(string Label, List<TrialBalanceRowDto> Accounts, decimal Total);
public record BalanceSheetDto(DateTime AsOf, BalanceSheetSectionDto Assets, BalanceSheetSectionDto Liabilities, BalanceSheetSectionDto Equity, decimal NetIncome, bool IsBalanced);

public record ProfitAndLossDto(DateTime From, DateTime To, List<TrialBalanceRowDto> Revenue, decimal TotalRevenue, List<TrialBalanceRowDto> Expenses, decimal TotalExpenses, decimal NetIncome);

public record CashBookLineDto(DateTime Date, string EntryNumber, string Description, string SourceType, decimal In, decimal Out, decimal RunningBalance);
public record CashBookDto(decimal OpeningBalance, List<CashBookLineDto> Lines, decimal ClosingBalance, decimal TotalIn, decimal TotalOut);

public record CashFlowLineDto(string Label, decimal Amount);
public record CashFlowDto(DateTime From, DateTime To, decimal OpeningCash, List<CashFlowLineDto> Lines, decimal NetChange, decimal ClosingCash);
