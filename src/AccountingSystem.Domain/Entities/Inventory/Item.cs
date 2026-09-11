using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Tax;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Inventory;

public class Item : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string SKU { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public ItemType Type { get; set; }

    public decimal SalesPrice { get; set; }
    public decimal PurchaseCost { get; set; }

    public Guid? IncomeAccountId { get; set; }
    public Account? IncomeAccount { get; set; }
    public Guid? ExpenseAccountId { get; set; }
    public Account? ExpenseAccount { get; set; }
    public Guid? InventoryAssetAccountId { get; set; }
    public Account? InventoryAssetAccount { get; set; }

    public InventoryValuationMethod ValuationMethod { get; set; } = InventoryValuationMethod.WeightedAverage;
    public decimal QuantityOnHand { get; set; }
    public decimal AverageCost { get; set; }
    public decimal ReorderPoint { get; set; }

    public Guid? DefaultTaxCodeId { get; set; }
    public TaxCode? DefaultTaxCode { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
}

public class StockTransaction : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public DateTime TransactionDate { get; set; }
    public StockTransactionType Type { get; set; }
    /// <summary>Positive for receipts, negative for issues.</summary>
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost => Quantity * UnitCost;

    public decimal RunningQuantity { get; set; }
    public decimal RunningValue { get; set; }

    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
}
