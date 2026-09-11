using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Banking;
using AccountingSystem.Domain.Entities.Parties;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Purchases;

public class VendorPayment : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string PaymentNumber { get; set; } = null!;
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

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

    public ICollection<VendorPaymentApplication> Applications { get; set; } = new List<VendorPaymentApplication>();
}

public class VendorPaymentApplication : BaseEntity
{
    public Guid VendorPaymentId { get; set; }
    public VendorPayment VendorPayment { get; set; } = null!;

    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public decimal AmountApplied { get; set; }
}

public class VendorCredit : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string VendorCreditNumber { get; set; } = null!;
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Reason { get; set; }
    public Guid? JournalEntryId { get; set; }

    public ICollection<VendorCreditApplication> Applications { get; set; } = new List<VendorCreditApplication>();
}

public class VendorCreditApplication : BaseEntity
{
    public Guid VendorCreditId { get; set; }
    public VendorCredit VendorCredit { get; set; } = null!;

    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public decimal AmountApplied { get; set; }
}
