using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Banking;

public class BankAccount : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;
    public string? BankName { get; set; }
    public string? AccountNumberMasked { get; set; }
    public string? RoutingNumber { get; set; }

    public Guid GLAccountId { get; set; }
    public Account GLAccount { get; set; } = null!;

    public string CurrencyCode { get; set; } = "USD";
    public decimal OpeningBalance { get; set; }
    public DateTime OpeningBalanceDate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}

public class BankTransaction : AuditableEntity
{
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public DateTime TransactionDate { get; set; }
    public string Description { get; set; } = null!;
    public BankTransactionType Type { get; set; }
    /// <summary>Positive for deposits, negative for withdrawals.</summary>
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }

    public Guid? JournalEntryId { get; set; }

    public bool IsReconciled { get; set; }
    public Guid? BankReconciliationId { get; set; }
    public BankReconciliation? BankReconciliation { get; set; }
}

public class BankReconciliation : AuditableEntity
{
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public DateTime StatementDate { get; set; }
    public decimal StatementBeginningBalance { get; set; }
    public decimal StatementEndingBalance { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletedBy { get; set; }

    public ICollection<BankTransaction> ReconciledTransactions { get; set; } = new List<BankTransaction>();

    public decimal ClearedBalance => ReconciledTransactions.Sum(t => t.Amount) + StatementBeginningBalance;
    public decimal Difference => StatementEndingBalance - ClearedBalance;
}
