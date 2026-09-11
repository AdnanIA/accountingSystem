using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Entities.Parties;
using AccountingSystem.Domain.Entities.Tax;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Purchases;

public class Bill : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string BillNumber { get; set; } = null!;
    public string? VendorInvoiceNumber { get; set; }
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public DateTime BillDate { get; set; }
    public DateTime DueDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.Draft;

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRateToBase { get; set; } = 1m;

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance => Total - AmountPaid;

    public string? Memo { get; set; }
    public Guid? JournalEntryId { get; set; }

    public ICollection<BillLine> Lines { get; set; } = new List<BillLine>();
    public ICollection<VendorPaymentApplication> PaymentApplications { get; set; } = new List<VendorPaymentApplication>();
}

public class BillLine : BaseEntity
{
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public int LineNumber { get; set; }
    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }
    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }

    public Guid? TaxCodeId { get; set; }
    public TaxCode? TaxCode { get; set; }
    public decimal TaxAmount { get; set; }

    /// <summary>Expense/asset account this line posts to; defaults to the Item's expense account.</summary>
    public Guid? ExpenseAccountId { get; set; }

    public decimal LineSubTotal => Math.Round(Quantity * UnitCost, 2);
    public decimal LineTotal => LineSubTotal + TaxAmount;
}
