using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IPeriodClosingService
{
    Task CloseFiscalPeriodAsync(Guid companyId, Guid fiscalPeriodId, string? closedBy, CancellationToken ct = default);
    Task ReopenFiscalPeriodAsync(Guid companyId, Guid fiscalPeriodId, string? reopenedBy, CancellationToken ct = default);

    /// <summary>Zeroes all revenue and expense accounts into Retained Earnings and closes every period in the year.</summary>
    Task CloseFiscalYearAsync(Guid companyId, Guid fiscalYearId, string? closedBy, CancellationToken ct = default);
}

public class PeriodClosingService : IPeriodClosingService
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journal;
    private readonly ISystemAccountResolver _systemAccounts;

    public PeriodClosingService(IApplicationDbContext db, IJournalEntryService journal, ISystemAccountResolver systemAccounts)
    {
        _db = db;
        _journal = journal;
        _systemAccounts = systemAccounts;
    }

    public async Task CloseFiscalPeriodAsync(Guid companyId, Guid fiscalPeriodId, string? closedBy, CancellationToken ct = default)
    {
        var period = await _db.FiscalPeriods.FirstOrDefaultAsync(p => p.Id == fiscalPeriodId && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Fiscal period not found.");

        period.Status = FiscalPeriodStatus.Closed;
        period.ClosedAtUtc = DateTime.UtcNow;
        period.ClosedBy = closedBy;

        await _db.SaveChangesAsync(ct);
    }

    public async Task ReopenFiscalPeriodAsync(Guid companyId, Guid fiscalPeriodId, string? reopenedBy, CancellationToken ct = default)
    {
        var period = await _db.FiscalPeriods.FirstOrDefaultAsync(p => p.Id == fiscalPeriodId && p.CompanyId == companyId, ct)
            ?? throw new DomainException("Fiscal period not found.");

        period.Status = FiscalPeriodStatus.Open;
        period.ClosedAtUtc = null;
        period.ClosedBy = null;

        await _db.SaveChangesAsync(ct);
    }

    public async Task CloseFiscalYearAsync(Guid companyId, Guid fiscalYearId, string? closedBy, CancellationToken ct = default)
    {
        var year = await _db.FiscalYears.Include(y => y.Periods).FirstOrDefaultAsync(y => y.Id == fiscalYearId && y.CompanyId == companyId, ct)
            ?? throw new DomainException("Fiscal year not found.");

        if (year.IsClosed)
            throw new DomainException("Fiscal year is already closed.");

        var periodIds = year.Periods.Select(p => p.Id).ToList();

        var pnlAccountBalances = await _db.JournalEntryLines
            .Where(l => l.JournalEntry.CompanyId == companyId
                && periodIds.Contains(l.JournalEntry.FiscalPeriodId)
                && l.JournalEntry.Status == JournalEntryStatus.Posted
                && (l.Account.Type == AccountType.Revenue || l.Account.Type == AccountType.Expense))
            .GroupBy(l => new { l.AccountId, l.Account.Type })
            .Select(g => new { g.Key.AccountId, g.Key.Type, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);

        var retainedEarningsId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.RetainedEarnings, ct);
        var lines = new List<JournalEntryLine>();
        decimal netIncome = 0;

        foreach (var b in pnlAccountBalances)
        {
            var netCredit = b.Credit - b.Debit; // revenue accounts normally net credit, expense accounts normally net debit (i.e. negative here)
            netIncome += netCredit;

            if (netCredit > 0)
                lines.Add(new JournalEntryLine { AccountId = b.AccountId, Debit = netCredit, Credit = 0, Description = "Year-end closing" });
            else if (netCredit < 0)
                lines.Add(new JournalEntryLine { AccountId = b.AccountId, Debit = 0, Credit = -netCredit, Description = "Year-end closing" });
        }

        if (netIncome > 0)
            lines.Add(new JournalEntryLine { AccountId = retainedEarningsId, Debit = 0, Credit = netIncome, Description = "Net income to retained earnings" });
        else if (netIncome < 0)
            lines.Add(new JournalEntryLine { AccountId = retainedEarningsId, Debit = -netIncome, Credit = 0, Description = "Net loss to retained earnings" });

        if (lines.Count > 0)
            await _journal.CreateSystemEntryAsync(companyId, year.EndDate, JournalSourceType.PeriodClosing, year.Id, $"Closing entries for {year.Name}", lines, closedBy, ct);

        foreach (var period in year.Periods)
        {
            period.Status = FiscalPeriodStatus.Closed;
            period.ClosedAtUtc = DateTime.UtcNow;
            period.ClosedBy = closedBy;
        }

        year.IsClosed = true;
        year.ClosedAtUtc = DateTime.UtcNow;
        year.ClosedBy = closedBy;

        await _db.SaveChangesAsync(ct);
    }
}
