using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Accounting;

/// <summary>A node in the Chart of Accounts.</summary>
public class Account : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public AccountType Type { get; set; }
    public string? SubType { get; set; }

    public Guid? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    public ICollection<Account> ChildAccounts { get; set; } = new List<Account>();

    public bool IsActive { get; set; } = true;
    /// <summary>System accounts (e.g. AR Control, AP Control, Retained Earnings) cannot be deleted and are managed by posting logic.</summary>
    public bool IsSystemAccount { get; set; }
    public string? SystemAccountKey { get; set; }

    public string? CurrencyCode { get; set; }

    public NormalBalance NormalBalance => Type is AccountType.Asset or AccountType.Expense
        ? NormalBalance.Debit
        : NormalBalance.Credit;

    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();
}
