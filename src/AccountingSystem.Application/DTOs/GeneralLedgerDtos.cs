using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Application.DTOs;

public record JournalLineRequest(Guid AccountId, decimal Debit, decimal Credit, string? Description, Guid? CustomerId = null, Guid? VendorId = null);

public record CreateJournalEntryRequest(
    DateTime EntryDate,
    string? Memo,
    List<JournalLineRequest> Lines,
    JournalSourceType SourceType = JournalSourceType.Manual,
    Guid? SourceId = null,
    string CurrencyCode = "USD",
    decimal ExchangeRateToBase = 1m);

public record AccountDto(Guid Id, string Code, string Name, AccountType Type, string? SubType, Guid? ParentAccountId, bool IsActive, NormalBalance NormalBalance);

public record CreateAccountRequest(string Code, string Name, AccountType Type, string? SubType, Guid? ParentAccountId, string? Description);

public record JournalEntryDto(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    JournalEntryStatus Status,
    JournalSourceType SourceType,
    string? Memo,
    decimal TotalDebit,
    decimal TotalCredit,
    List<JournalEntryLineDto> Lines);

public record JournalEntryLineDto(Guid AccountId, string AccountCode, string AccountName, decimal Debit, decimal Credit, string? Description);
