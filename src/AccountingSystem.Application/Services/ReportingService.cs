using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IReportingService
{
    Task<TrialBalanceReport> GetTrialBalanceAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default);
    Task<IncomeStatementReport> GetIncomeStatementAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<BalanceSheetReport> GetBalanceSheetAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default);
    Task<GeneralLedgerReport> GetGeneralLedgerAsync(Guid companyId, Guid accountId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<AgingReport> GetArAgingAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default);
    Task<AgingReport> GetApAgingAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default);

    /// <summary>Simplified indirect-method cash flow statement. Financing activities are not separately tracked and report as zero.</summary>
    Task<CashFlowReport> GetCashFlowAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

/// <summary>
/// Note: all aggregation (Sum/GroupBy) below happens client-side after materializing raw line rows.
/// SQLite's EF Core provider cannot translate decimal Sum() into SQL, and doing the aggregation in
/// .NET keeps the same code portable to SQL Server without a provider-specific code path.
/// </summary>
public class ReportingService : IReportingService
{
    private readonly IApplicationDbContext _db;

    public ReportingService(IApplicationDbContext db) => _db = db;

    private record LineRow(Guid AccountId, string Code, string Name, AccountType Type, decimal Debit, decimal Credit);

    private async Task<List<LineRow>> GetPostedLinesAsync(Guid companyId, DateTime? fromDate, DateTime toDate, CancellationToken ct)
    {
        var query = _db.JournalEntryLines
            .Where(l => l.JournalEntry.CompanyId == companyId && l.JournalEntry.Status == JournalEntryStatus.Posted && l.JournalEntry.EntryDate <= toDate);

        if (fromDate.HasValue)
            query = query.Where(l => l.JournalEntry.EntryDate >= fromDate.Value);

        return await query
            .Select(l => new LineRow(l.AccountId, l.Account.Code, l.Account.Name, l.Account.Type, l.Debit, l.Credit))
            .ToListAsync(ct);
    }

    public async Task<TrialBalanceReport> GetTrialBalanceAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default)
    {
        var lines = await GetPostedLinesAsync(companyId, null, asOfDate, ct);

        var rows = lines.GroupBy(l => new { l.AccountId, l.Code, l.Name, l.Type })
            .Select(g =>
            {
                var net = g.Sum(x => x.Debit) - g.Sum(x => x.Credit);
                return net >= 0
                    ? new TrialBalanceRow(g.Key.AccountId, g.Key.Code, g.Key.Name, g.Key.Type.ToString(), net, 0)
                    : new TrialBalanceRow(g.Key.AccountId, g.Key.Code, g.Key.Name, g.Key.Type.ToString(), 0, -net);
            })
            .OrderBy(r => r.AccountCode)
            .ToList();

        return new TrialBalanceReport(asOfDate, rows, rows.Sum(r => r.Debit), rows.Sum(r => r.Credit));
    }

    public async Task<IncomeStatementReport> GetIncomeStatementAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var lines = (await GetPostedLinesAsync(companyId, startDate, endDate, ct))
            .Where(l => l.Type is AccountType.Revenue or AccountType.Expense)
            .ToList();

        var revenues = lines.Where(l => l.Type == AccountType.Revenue)
            .GroupBy(l => new { l.Code, l.Name })
            .Select(g => new IncomeStatementRow(g.Key.Code, g.Key.Name, g.Sum(x => x.Credit) - g.Sum(x => x.Debit)))
            .OrderBy(r => r.AccountCode).ToList();

        var expenses = lines.Where(l => l.Type == AccountType.Expense)
            .GroupBy(l => new { l.Code, l.Name })
            .Select(g => new IncomeStatementRow(g.Key.Code, g.Key.Name, g.Sum(x => x.Debit) - g.Sum(x => x.Credit)))
            .OrderBy(r => r.AccountCode).ToList();

        var totalRevenue = revenues.Sum(r => r.Amount);
        var totalExpense = expenses.Sum(r => r.Amount);

        return new IncomeStatementReport(startDate, endDate, revenues, totalRevenue, expenses, totalExpense, totalRevenue - totalExpense);
    }

    public async Task<BalanceSheetReport> GetBalanceSheetAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default)
    {
        var lines = await GetPostedLinesAsync(companyId, null, asOfDate, ct);

        List<BalanceSheetRow> RowsFor(AccountType type, bool creditNormal) =>
            lines.Where(l => l.Type == type)
                .GroupBy(l => new { l.Code, l.Name })
                .Select(g => new BalanceSheetRow(g.Key.Code, g.Key.Name, creditNormal ? g.Sum(x => x.Credit) - g.Sum(x => x.Debit) : g.Sum(x => x.Debit) - g.Sum(x => x.Credit)))
                .OrderBy(r => r.AccountCode).ToList();

        var assets = RowsFor(AccountType.Asset, creditNormal: false);
        var liabilities = RowsFor(AccountType.Liability, creditNormal: true);
        var equity = RowsFor(AccountType.Equity, creditNormal: true);

        var netIncomeYtd = lines.Where(l => l.Type == AccountType.Revenue).Sum(l => l.Credit - l.Debit)
            - lines.Where(l => l.Type == AccountType.Expense).Sum(l => l.Debit - l.Credit);

        var totalAssets = assets.Sum(a => a.Amount);
        var totalLiabilities = liabilities.Sum(l => l.Amount);
        var totalEquityExcludingNetIncome = equity.Sum(e => e.Amount);
        var totalEquity = totalEquityExcludingNetIncome + netIncomeYtd;

        return new BalanceSheetReport(asOfDate, assets, totalAssets, liabilities, totalLiabilities, equity, totalEquityExcludingNetIncome, netIncomeYtd, totalEquity, totalLiabilities + totalEquity);
    }

    public async Task<GeneralLedgerReport> GetGeneralLedgerAsync(Guid companyId, Guid accountId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.CompanyId == companyId, ct)
            ?? throw new DomainException("Account not found.");

        var priorLines = await _db.JournalEntryLines
            .Where(l => l.AccountId == accountId && l.JournalEntry.Status == JournalEntryStatus.Posted && l.JournalEntry.EntryDate < startDate)
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync(ct);
        var openingMovement = priorLines.Sum(l => l.Debit - l.Credit);

        var openingBalance = account.NormalBalance == NormalBalance.Debit ? openingMovement : -openingMovement;

        var movements = await _db.JournalEntryLines
            .Where(l => l.AccountId == accountId && l.JournalEntry.Status == JournalEntryStatus.Posted
                && l.JournalEntry.EntryDate >= startDate && l.JournalEntry.EntryDate <= endDate)
            .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.LineNumber)
            .Select(l => new { l.JournalEntry.EntryDate, l.JournalEntry.EntryNumber, l.JournalEntryId, l.Description, l.Debit, l.Credit })
            .ToListAsync(ct);

        var runningBalance = openingBalance;
        var lines = new List<GeneralLedgerLine>();
        foreach (var m in movements)
        {
            var delta = account.NormalBalance == NormalBalance.Debit ? m.Debit - m.Credit : m.Credit - m.Debit;
            runningBalance += delta;
            lines.Add(new GeneralLedgerLine(m.EntryDate, m.EntryNumber, m.Description, m.Debit, m.Credit, runningBalance, m.JournalEntryId));
        }

        return new GeneralLedgerReport(account.Code, account.Name, openingBalance, lines, runningBalance);
    }

    public async Task<AgingReport> GetArAgingAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default)
    {
        var invoices = await _db.Invoices
            .Where(i => i.CompanyId == companyId && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Voided)
            .Select(i => new { i.CustomerId, i.Customer.Name, i.DueDate, i.Total, i.AmountPaid })
            .ToListAsync(ct);

        var rows = BuildAging(invoices.Select(i => (i.CustomerId, i.Name, i.DueDate, i.Total - i.AmountPaid)), asOfDate);
        return new AgingReport(asOfDate, rows, rows.Sum(r => r.TotalDue));
    }

    public async Task<AgingReport> GetApAgingAsync(Guid companyId, DateTime asOfDate, CancellationToken ct = default)
    {
        var bills = await _db.Bills
            .Where(b => b.CompanyId == companyId && b.Status != BillStatus.Paid && b.Status != BillStatus.Voided)
            .Select(b => new { b.VendorId, b.Vendor.Name, b.DueDate, b.Total, b.AmountPaid })
            .ToListAsync(ct);

        var rows = BuildAging(bills.Select(b => (b.VendorId, b.Name, b.DueDate, b.Total - b.AmountPaid)), asOfDate);
        return new AgingReport(asOfDate, rows, rows.Sum(r => r.TotalDue));
    }

    public async Task<CashFlowReport> GetCashFlowAsync(Guid companyId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var incomeStatement = await GetIncomeStatementAsync(companyId, startDate, endDate, ct);

        var depreciationEntries = await _db.DepreciationEntries
            .Where(d => d.PeriodDate >= startDate && d.PeriodDate <= endDate && d.FixedAsset.CompanyId == companyId)
            .Select(d => d.Amount)
            .ToListAsync(ct);
        var depreciation = depreciationEntries.Sum();

        var arAccountId = await _db.Accounts.Where(a => a.CompanyId == companyId && a.SystemAccountKey == SystemAccountKeys.AccountsReceivable).Select(a => a.Id).FirstOrDefaultAsync(ct);
        var apAccountId = await _db.Accounts.Where(a => a.CompanyId == companyId && a.SystemAccountKey == SystemAccountKeys.AccountsPayable).Select(a => a.Id).FirstOrDefaultAsync(ct);
        var inventoryAccountIds = await _db.Items.Where(i => i.CompanyId == companyId && i.InventoryAssetAccountId != null)
            .Select(i => i.InventoryAssetAccountId!.Value).Distinct().ToListAsync(ct);
        var bankAccountIds = await _db.BankAccounts.Where(b => b.CompanyId == companyId).Select(b => b.GLAccountId).ToListAsync(ct);

        var arStart = await GetAccountsDebitBalanceAsOf(new[] { arAccountId }, startDate.AddDays(-1), ct);
        var arEnd = await GetAccountsDebitBalanceAsOf(new[] { arAccountId }, endDate, ct);
        var apStart = await GetAccountsCreditBalanceAsOf(new[] { apAccountId }, startDate.AddDays(-1), ct);
        var apEnd = await GetAccountsCreditBalanceAsOf(new[] { apAccountId }, endDate, ct);
        var invStart = await GetAccountsDebitBalanceAsOf(inventoryAccountIds, startDate.AddDays(-1), ct);
        var invEnd = await GetAccountsDebitBalanceAsOf(inventoryAccountIds, endDate, ct);
        var cashStart = await GetAccountsDebitBalanceAsOf(bankAccountIds, startDate.AddDays(-1), ct);
        var cashEnd = await GetAccountsDebitBalanceAsOf(bankAccountIds, endDate, ct);

        var changeInAr = arEnd - arStart;
        var changeInInventory = invEnd - invStart;
        var changeInAp = apEnd - apStart;

        var netCashFromOperations = incomeStatement.NetIncome + depreciation - changeInAr - changeInInventory + changeInAp;

        return new CashFlowReport(startDate, endDate, incomeStatement.NetIncome, depreciation, changeInAr, changeInInventory, changeInAp,
            netCashFromOperations, 0, 0, netCashFromOperations, cashStart, cashEnd);
    }

    private async Task<decimal> GetAccountsDebitBalanceAsOf(IEnumerable<Guid> accountIds, DateTime asOfDate, CancellationToken ct)
    {
        var ids = accountIds.Where(id => id != Guid.Empty).ToList();
        if (ids.Count == 0) return 0;

        var lines = await _db.JournalEntryLines
            .Where(l => ids.Contains(l.AccountId) && l.JournalEntry.Status == JournalEntryStatus.Posted && l.JournalEntry.EntryDate <= asOfDate)
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync(ct);

        return lines.Sum(l => l.Debit - l.Credit);
    }

    private async Task<decimal> GetAccountsCreditBalanceAsOf(IEnumerable<Guid> accountIds, DateTime asOfDate, CancellationToken ct)
    {
        var ids = accountIds.Where(id => id != Guid.Empty).ToList();
        if (ids.Count == 0) return 0;

        var lines = await _db.JournalEntryLines
            .Where(l => ids.Contains(l.AccountId) && l.JournalEntry.Status == JournalEntryStatus.Posted && l.JournalEntry.EntryDate <= asOfDate)
            .Select(l => new { l.Debit, l.Credit })
            .ToListAsync(ct);

        return lines.Sum(l => l.Credit - l.Debit);
    }

    private static List<AgingRow> BuildAging(IEnumerable<(Guid PartyId, string Name, DateTime DueDate, decimal Balance)> items, DateTime asOfDate)
    {
        return items
            .GroupBy(i => new { i.PartyId, i.Name })
            .Select(g =>
            {
                decimal current = 0, d30 = 0, d60 = 0, d90 = 0, over90 = 0;
                foreach (var item in g)
                {
                    var daysPastDue = (asOfDate.Date - item.DueDate.Date).Days;
                    if (daysPastDue <= 0) current += item.Balance;
                    else if (daysPastDue <= 30) d30 += item.Balance;
                    else if (daysPastDue <= 60) d60 += item.Balance;
                    else if (daysPastDue <= 90) d90 += item.Balance;
                    else over90 += item.Balance;
                }
                var bucket = new AgingBucket(current, d30, d60, d90, over90);
                return new AgingRow(g.Key.PartyId, g.Key.Name, current + d30 + d60 + d90 + over90, bucket);
            })
            .Where(r => r.TotalDue != 0)
            .OrderByDescending(r => r.TotalDue)
            .ToList();
    }
}
