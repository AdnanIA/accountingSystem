namespace AccountingSystem.Domain.Enums;

public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

public enum NormalBalance
{
    Debit = 1,
    Credit = 2
}

public enum FiscalPeriodStatus
{
    Open = 1,
    Closed = 2
}

public enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    Voided = 3
}

public enum JournalSourceType
{
    Manual = 1,
    SalesInvoice = 2,
    CustomerPayment = 3,
    CreditMemo = 4,
    VendorBill = 5,
    VendorPayment = 6,
    VendorCredit = 7,
    BankTransaction = 8,
    Depreciation = 9,
    OpeningBalance = 10,
    Adjustment = 11,
    InventoryAdjustment = 12,
    PeriodClosing = 13
}

public enum InvoiceStatus
{
    Draft = 1,
    Sent = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Voided = 6
}

public enum BillStatus
{
    Draft = 1,
    Approved = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Voided = 5
}

public enum PaymentMethod
{
    Cash = 1,
    Check = 2,
    BankTransfer = 3,
    CreditCard = 4,
    DebitCard = 5,
    Other = 6
}

public enum BankTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    Fee = 4,
    Interest = 5
}

public enum ItemType
{
    Inventory = 1,
    Service = 2,
    NonInventory = 3
}

public enum InventoryValuationMethod
{
    FIFO = 1,
    WeightedAverage = 2
}

public enum StockTransactionType
{
    PurchaseReceipt = 1,
    SaleIssue = 2,
    AdjustmentIncrease = 3,
    AdjustmentDecrease = 4,
    OpeningStock = 5
}

public enum TaxType
{
    Sales = 1,
    Purchase = 2
}

public enum AssetStatus
{
    Active = 1,
    FullyDepreciated = 2,
    Disposed = 3
}

public enum DepreciationMethod
{
    StraightLine = 1,
    DecliningBalance = 2
}

public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Post = 4,
    Void = 5,
    Login = 6,
    ClosePeriod = 7,
    ReopenPeriod = 8
}

public enum RecurrenceFrequency
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}
