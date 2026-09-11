using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IJournalEntryService
{
    /// <summary>Creates a journal entry and immediately posts it (validates balance and open period).</summary>
    Task<JournalEntry> CreateAndPostAsync(Guid companyId, CreateJournalEntryRequest request, string? postedBy, CancellationToken ct = default);

    /// <summary>Creates a fully-formed, already-balanced entry from a subledger transaction (invoice, bill, payment, ...).</summary>
    Task<JournalEntry> CreateSystemEntryAsync(Guid companyId, DateTime entryDate, JournalSourceType sourceType, Guid sourceId, string? memo, List<JournalEntryLine> lines, string? postedBy, CancellationToken ct = default);

    Task<JournalEntry> ReverseAsync(Guid journalEntryId, DateTime reversalDate, string? reversedBy, CancellationToken ct = default);
}

public class JournalEntryService : IJournalEntryService
{
    private readonly IApplicationDbContext _db;
    private readonly IFiscalPeriodService _periods;
    private readonly INumberSequenceService _numbers;

    public JournalEntryService(IApplicationDbContext db, IFiscalPeriodService periods, INumberSequenceService numbers)
    {
        _db = db;
        _periods = periods;
        _numbers = numbers;
    }

    public async Task<JournalEntry> CreateAndPostAsync(Guid companyId, CreateJournalEntryRequest request, string? postedBy, CancellationToken ct = default)
    {
        if (request.Lines is null || request.Lines.Count < 2)
            throw new DomainException("A journal entry requires at least two lines.");

        var lines = request.Lines.Select((l, i) => new JournalEntryLine
        {
            LineNumber = i + 1,
            AccountId = l.AccountId,
            Debit = Math.Round(l.Debit, 2),
            Credit = Math.Round(l.Credit, 2),
            Description = l.Description,
            CustomerId = l.CustomerId,
            VendorId = l.VendorId
        }).ToList();

        return await BuildAndPostAsync(companyId, request.EntryDate, request.SourceType, request.SourceId, request.Memo, lines, postedBy, request.CurrencyCode, request.ExchangeRateToBase, ct);
    }

    public async Task<JournalEntry> CreateSystemEntryAsync(Guid companyId, DateTime entryDate, JournalSourceType sourceType, Guid sourceId, string? memo, List<JournalEntryLine> lines, string? postedBy, CancellationToken ct = default)
        => await BuildAndPostAsync(companyId, entryDate, sourceType, sourceId, memo, lines, postedBy, "USD", 1m, ct);

    private async Task<JournalEntry> BuildAndPostAsync(Guid companyId, DateTime entryDate, JournalSourceType sourceType, Guid? sourceId, string? memo, List<JournalEntryLine> lines, string? postedBy, string currencyCode, decimal exchangeRate, CancellationToken ct)
    {
        foreach (var line in lines)
        {
            if (line.Debit < 0 || line.Credit < 0)
                throw new DomainException("Journal entry line amounts cannot be negative.");
            if (line.Debit > 0 && line.Credit > 0)
                throw new DomainException("A journal entry line cannot have both a debit and a credit amount.");
            if (line.Debit == 0 && line.Credit == 0)
                throw new DomainException("A journal entry line must have a non-zero debit or credit amount.");
        }

        var totalDebit = Math.Round(lines.Sum(l => l.Debit), 2);
        var totalCredit = Math.Round(lines.Sum(l => l.Credit), 2);
        if (totalDebit != totalCredit)
            throw new DomainException($"Journal entry is not balanced: total debits {totalDebit} != total credits {totalCredit}.");

        var period = await _periods.GetOpenPeriodForDateAsync(companyId, entryDate, ct);
        var entryNumber = await _numbers.GetNextNumberAsync(companyId, "JE", "JE", ct);

        var entry = new JournalEntry
        {
            CompanyId = companyId,
            EntryNumber = entryNumber,
            EntryDate = entryDate,
            FiscalPeriodId = period.Id,
            Status = JournalEntryStatus.Posted,
            SourceType = sourceType,
            SourceId = sourceId,
            Memo = memo,
            CurrencyCode = currencyCode,
            ExchangeRateToBase = exchangeRate,
            PostedAtUtc = DateTime.UtcNow,
            PostedBy = postedBy,
            Lines = lines
        };

        _db.JournalEntries.Add(entry);
        return entry;
    }

    public async Task<JournalEntry> ReverseAsync(Guid journalEntryId, DateTime reversalDate, string? reversedBy, CancellationToken ct = default)
    {
        var original = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.Id == journalEntryId, ct)
            ?? throw new DomainException("Journal entry not found.");

        if (original.Status != JournalEntryStatus.Posted)
            throw new DomainException("Only posted journal entries can be reversed.");
        if (original.IsReversed)
            throw new DomainException("This journal entry has already been reversed.");

        var period = await _periods.GetOpenPeriodForDateAsync(original.CompanyId, reversalDate, ct);
        var entryNumber = await _numbers.GetNextNumberAsync(original.CompanyId, "JE", "JE", ct);

        var reversal = new JournalEntry
        {
            CompanyId = original.CompanyId,
            EntryNumber = entryNumber,
            EntryDate = reversalDate,
            FiscalPeriodId = period.Id,
            Status = JournalEntryStatus.Posted,
            SourceType = original.SourceType,
            SourceId = original.SourceId,
            Memo = $"Reversal of {original.EntryNumber}",
            CurrencyCode = original.CurrencyCode,
            ExchangeRateToBase = original.ExchangeRateToBase,
            PostedAtUtc = DateTime.UtcNow,
            PostedBy = reversedBy,
            ReversalOfEntryId = original.Id,
            Lines = original.Lines.Select((l, i) => new JournalEntryLine
            {
                LineNumber = i + 1,
                AccountId = l.AccountId,
                Debit = l.Credit,
                Credit = l.Debit,
                Description = l.Description,
                CustomerId = l.CustomerId,
                VendorId = l.VendorId
            }).ToList()
        };

        original.IsReversed = true;
        _db.JournalEntries.Add(reversal);
        return reversal;
    }
}
