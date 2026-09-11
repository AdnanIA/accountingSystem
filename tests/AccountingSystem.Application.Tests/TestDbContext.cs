using AccountingSystem.Application.Common.Interfaces;
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

namespace AccountingSystem.Application.Tests;

/// <summary>Minimal EF Core InMemory context implementing IApplicationDbContext, used purely for unit tests.</summary>
public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<CustomerPaymentApplication> CustomerPaymentApplications => Set<CustomerPaymentApplication>();
    public DbSet<CreditMemo> CreditMemos => Set<CreditMemo>();
    public DbSet<CreditMemoApplication> CreditMemoApplications => Set<CreditMemoApplication>();

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillLine> BillLines => Set<BillLine>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();
    public DbSet<VendorPaymentApplication> VendorPaymentApplications => Set<VendorPaymentApplication>();
    public DbSet<VendorCredit> VendorCredits => Set<VendorCredit>();
    public DbSet<VendorCreditApplication> VendorCreditApplications => Set<VendorCreditApplication>();

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();

    public DbSet<Item> Items => Set<Item>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();

    public DbSet<FixedAsset> FixedAssets => Set<FixedAsset>();
    public DbSet<DepreciationEntry> DepreciationEntries => Set<DepreciationEntry>();

    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<decimal>().HavePrecision(18, 6);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Currency>().HasKey(c => c.Code);
    }
}
