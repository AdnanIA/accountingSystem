using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Organization;
using AccountingSystem.Domain.Entities.Parties;
using AccountingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingSystem.Application.Tests;

public class InvoiceServiceTests
{
    [Fact]
    public async Task PostAsync_CreatesBalancedJournalEntry_AndUpdatesTrialBalance()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new TestDbContext(options);
        var companyId = Guid.NewGuid();

        var year = new FiscalYear { CompanyId = companyId, Name = "FY2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
        year.Periods.Add(new FiscalPeriod { CompanyId = companyId, FiscalYear = year, Name = "Jan 2026", PeriodNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31), Status = FiscalPeriodStatus.Open });
        db.FiscalYears.Add(year);

        var ar = new Account { CompanyId = companyId, Code = "1100", Name = "Accounts Receivable", Type = AccountType.Asset, SystemAccountKey = SystemAccountKeys.AccountsReceivable };
        var revenue = new Account { CompanyId = companyId, Code = "4000", Name = "Sales Revenue", Type = AccountType.Revenue, SystemAccountKey = SystemAccountKeys.DefaultSalesRevenue };
        db.Accounts.AddRange(ar, revenue);

        var customer = new Customer { CompanyId = companyId, Code = "CUST-1", Name = "Acme Corp" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var journalService = new JournalEntryService(db, new FiscalPeriodService(db), new NumberSequenceService(db));
        var invoiceService = new InvoiceService(db, journalService, new NumberSequenceService(db), new SystemAccountResolver(db), new InventoryService(db));

        var createRequest = new CreateInvoiceRequest(
            customer.Id, new DateTime(2026, 1, 10), new DateTime(2026, 2, 9), "Consulting", null,
            new List<InvoiceLineRequest> { new(null, "Consulting services", 10m, 150m, 0m, null, null) });

        var invoice = await invoiceService.CreateDraftAsync(companyId, createRequest);
        Assert.Equal(1500m, invoice.Total);

        var posted = await invoiceService.PostAsync(companyId, invoice.Id, "tester");
        Assert.Equal(InvoiceStatus.Sent, posted.Status);
        Assert.NotNull(posted.JournalEntryId);

        var reportingService = new ReportingService(db);
        var trialBalance = await reportingService.GetTrialBalanceAsync(companyId, new DateTime(2026, 1, 31));

        Assert.Equal(trialBalance.TotalDebit, trialBalance.TotalCredit);
        Assert.Contains(trialBalance.Rows, r => r.AccountCode == "1100" && r.Debit == 1500m);
        Assert.Contains(trialBalance.Rows, r => r.AccountCode == "4000" && r.Credit == 1500m);
    }
}
