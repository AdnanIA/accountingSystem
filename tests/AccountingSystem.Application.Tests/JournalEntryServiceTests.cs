using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Organization;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingSystem.Application.Tests;

public class JournalEntryServiceTests
{
    private static TestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private static async Task<(TestDbContext db, Guid companyId, Guid cashAccountId, Guid revenueAccountId)> SeedCompanyAsync()
    {
        var db = NewContext();
        var companyId = Guid.NewGuid();

        var year = new FiscalYear { CompanyId = companyId, Name = "FY2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 12, 31) };
        var period = new FiscalPeriod { CompanyId = companyId, FiscalYear = year, Name = "Jan 2026", PeriodNumber = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31), Status = FiscalPeriodStatus.Open };
        year.Periods.Add(period);
        db.FiscalYears.Add(year);

        var cash = new Account { CompanyId = companyId, Code = "1000", Name = "Cash", Type = AccountType.Asset };
        var revenue = new Account { CompanyId = companyId, Code = "4000", Name = "Sales Revenue", Type = AccountType.Revenue };
        db.Accounts.AddRange(cash, revenue);

        await db.SaveChangesAsync();
        return (db, companyId, cash.Id, revenue.Id);
    }

    [Fact]
    public async Task CreateAndPostAsync_WithUnbalancedLines_ThrowsDomainException()
    {
        var (db, companyId, cashId, revenueId) = await SeedCompanyAsync();
        var service = new JournalEntryService(db, new FiscalPeriodService(db), new NumberSequenceService(db));

        var request = new CreateJournalEntryRequest(
            new DateTime(2026, 1, 15), "Unbalanced test",
            new List<JournalLineRequest> { new(cashId, 100m, 0m, null), new(revenueId, 0m, 50m, null) });

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAndPostAsync(companyId, request, "tester"));
    }

    [Fact]
    public async Task CreateAndPostAsync_WithBalancedLines_PostsSuccessfully()
    {
        var (db, companyId, cashId, revenueId) = await SeedCompanyAsync();
        var service = new JournalEntryService(db, new FiscalPeriodService(db), new NumberSequenceService(db));

        var request = new CreateJournalEntryRequest(
            new DateTime(2026, 1, 15), "Cash sale",
            new List<JournalLineRequest> { new(cashId, 100m, 0m, "Cash received"), new(revenueId, 0m, 100m, "Sale") });

        var entry = await service.CreateAndPostAsync(companyId, request, "tester");
        await db.SaveChangesAsync();

        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Equal(100m, entry.TotalDebit);
        Assert.Equal(100m, entry.TotalCredit);
        Assert.StartsWith("JE-", entry.EntryNumber);
    }

    [Fact]
    public async Task CreateAndPostAsync_ForDateOutsideAnyPeriod_ThrowsDomainException()
    {
        var (db, companyId, cashId, revenueId) = await SeedCompanyAsync();
        var service = new JournalEntryService(db, new FiscalPeriodService(db), new NumberSequenceService(db));

        var request = new CreateJournalEntryRequest(
            new DateTime(2030, 1, 15), "Out of period",
            new List<JournalLineRequest> { new(cashId, 100m, 0m, null), new(revenueId, 0m, 100m, null) });

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAndPostAsync(companyId, request, "tester"));
    }
}
