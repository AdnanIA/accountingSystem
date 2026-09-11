namespace AccountingSystem.Application.DTOs;

public record TrialBalanceRow(Guid AccountId, string AccountCode, string AccountName, string AccountType, decimal Debit, decimal Credit);
public record TrialBalanceReport(DateTime AsOfDate, List<TrialBalanceRow> Rows, decimal TotalDebit, decimal TotalCredit);

public record IncomeStatementRow(string AccountCode, string AccountName, decimal Amount);
public record IncomeStatementReport(DateTime StartDate, DateTime EndDate, List<IncomeStatementRow> Revenues, decimal TotalRevenue, List<IncomeStatementRow> Expenses, decimal TotalExpense, decimal NetIncome);

public record BalanceSheetRow(string AccountCode, string AccountName, decimal Amount);
public record BalanceSheetReport(
    DateTime AsOfDate,
    List<BalanceSheetRow> Assets, decimal TotalAssets,
    List<BalanceSheetRow> Liabilities, decimal TotalLiabilities,
    List<BalanceSheetRow> Equity, decimal TotalEquityExcludingNetIncome,
    decimal NetIncomeYearToDate,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity);

public record GeneralLedgerLine(DateTime Date, string EntryNumber, string? Description, decimal Debit, decimal Credit, decimal RunningBalance, Guid JournalEntryId);
public record GeneralLedgerReport(string AccountCode, string AccountName, decimal OpeningBalance, List<GeneralLedgerLine> Lines, decimal ClosingBalance);

public record AgingBucket(decimal Current, decimal Days1To30, decimal Days31To60, decimal Days61To90, decimal Over90);
public record AgingRow(Guid PartyId, string PartyName, decimal TotalDue, AgingBucket Buckets);
public record AgingReport(DateTime AsOfDate, List<AgingRow> Rows, decimal GrandTotal);

public record CashFlowReport(
    DateTime StartDate, DateTime EndDate,
    decimal NetIncome,
    decimal Depreciation,
    decimal ChangeInAccountsReceivable,
    decimal ChangeInInventory,
    decimal ChangeInAccountsPayable,
    decimal NetCashFromOperations,
    decimal NetCashFromInvesting,
    decimal NetCashFromFinancing,
    decimal NetChangeInCash,
    decimal BeginningCash,
    decimal EndingCash);
