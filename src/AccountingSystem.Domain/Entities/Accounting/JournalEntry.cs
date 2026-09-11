using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Organization;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Accounting;

public class JournalEntry : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    /// <summary>Sequential, human-readable number per company, e.g. JE-000123.</summary>
    public string EntryNumber { get; set; } = null!;
    public DateTime EntryDate { get; set; }

    public Guid FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;
    public JournalSourceType SourceType { get; set; } = JournalSourceType.Manual;
    /// <summary>Id of the originating document (Invoice, Bill, Payment, ...), when SourceType != Manual.</summary>
    public Guid? SourceId { get; set; }

    public string? Memo { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRateToBase { get; set; } = 1m;

    public DateTime? PostedAtUtc { get; set; }
    public string? PostedBy { get; set; }

    public Guid? ReversalOfEntryId { get; set; }
    public JournalEntry? ReversalOfEntry { get; set; }
    public bool IsReversed { get; set; }

    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit;
}

public class JournalEntryLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }

    /// <summary>Optional link to a Customer or Vendor for subledger (AR/AP control account) reporting.</summary>
    public Guid? CustomerId { get; set; }
    public Guid? VendorId { get; set; }
}
