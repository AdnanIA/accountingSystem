using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Banking;
using AccountingSystem.Domain.Entities.Parties;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Sales;

public class CustomerPayment : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string PaymentNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? ReferenceNumber { get; set; }

    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRateToBase { get; set; } = 1m;

    public decimal UnappliedAmount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? Memo { get; set; }

    public ICollection<CustomerPaymentApplication> Applications { get; set; } = new List<CustomerPaymentApplication>();
}

public class CustomerPaymentApplication : BaseEntity
{
    public Guid CustomerPaymentId { get; set; }
    public CustomerPayment CustomerPayment { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal AmountApplied { get; set; }
}

public class CreditMemo : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string CreditMemoNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Reason { get; set; }
    public Guid? JournalEntryId { get; set; }

    public ICollection<CreditMemoApplication> Applications { get; set; } = new List<CreditMemoApplication>();
}

public class CreditMemoApplication : BaseEntity
{
    public Guid CreditMemoId { get; set; }
    public CreditMemo CreditMemo { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal AmountApplied { get; set; }
}
