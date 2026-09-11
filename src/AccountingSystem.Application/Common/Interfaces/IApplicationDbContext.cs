using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Assets;
using AccountingSystem.Domain.Entities.Audit;
using AccountingSystem.Domain.Entities.Banking;
using AccountingSystem.Domain.Entities.Budgeting;
using AccountingSystem.Domain.Entities.Currency;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Entities.Organization;
using AccountingSystem.Domain.Entities.Parties;
using AccountingSystem.Domain.Entities.Purchases;
using AccountingSystem.Domain.Entities.Sales;
using AccountingSystem.Domain.Entities.Tax;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<FiscalYear> FiscalYears { get; }
    DbSet<FiscalPeriod> FiscalPeriods { get; }
    DbSet<NumberSequence> NumberSequences { get; }

    DbSet<Account> Accounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }

    DbSet<Customer> Customers { get; }
    DbSet<Vendor> Vendors { get; }

    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<CustomerPayment> CustomerPayments { get; }
    DbSet<CustomerPaymentApplication> CustomerPaymentApplications { get; }
    DbSet<CreditMemo> CreditMemos { get; }
    DbSet<CreditMemoApplication> CreditMemoApplications { get; }

    DbSet<Bill> Bills { get; }
    DbSet<BillLine> BillLines { get; }
    DbSet<VendorPayment> VendorPayments { get; }
    DbSet<VendorPaymentApplication> VendorPaymentApplications { get; }
    DbSet<VendorCredit> VendorCredits { get; }
    DbSet<VendorCreditApplication> VendorCreditApplications { get; }

    DbSet<BankAccount> BankAccounts { get; }
    DbSet<BankTransaction> BankTransactions { get; }
    DbSet<BankReconciliation> BankReconciliations { get; }

    DbSet<Item> Items { get; }
    DbSet<StockTransaction> StockTransactions { get; }

    DbSet<TaxCode> TaxCodes { get; }

    DbSet<FixedAsset> FixedAssets { get; }
    DbSet<DepreciationEntry> DepreciationEntries { get; }

    DbSet<Budget> Budgets { get; }
    DbSet<BudgetLine> BudgetLines { get; }

    DbSet<Currency> Currencies { get; }
    DbSet<ExchangeRate> ExchangeRates { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
