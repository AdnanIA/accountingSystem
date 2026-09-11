using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Entities.Parties;
using AccountingSystem.Domain.Entities.Tax;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Sales;

public class Invoice : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string InvoiceNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRateToBase { get; set; } = 1m;

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance => Total - AmountPaid;

    public string? Memo { get; set; }
    public string? Terms { get; set; }

    public Guid? JournalEntryId { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<CustomerPaymentApplication> PaymentApplications { get; set; } = new List<CustomerPaymentApplication>();
}

public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }

    public Guid? TaxCodeId { get; set; }
    public TaxCode? TaxCode { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Revenue account this line posts to; defaults to the Item's income account.</summary>
    public Guid? RevenueAccountId { get; set; }

    public decimal LineSubTotal => Math.Round(Quantity * UnitPrice * (1 - DiscountPercent / 100m), 2);
    public decimal LineTotal => LineSubTotal + TaxAmount;
}
