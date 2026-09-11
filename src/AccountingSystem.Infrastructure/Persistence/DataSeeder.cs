using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Currency;
using AccountingSystem.Domain.Entities.Organization;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Infrastructure.Persistence;

/// <summary>Seeds a demo company, chart of accounts, roles and an admin user so the API is usable immediately after first run.</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, RoleManager<IdentityRole<Guid>> roleManager, UserManager<ApplicationUser> userManager)
    {
        await db.Database.MigrateAsync();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        if (!await db.Currencies.AnyAsync())
        {
            db.Currencies.AddRange(
                new Currency { Code = "USD", Name = "US Dollar", Symbol = "$" },
                new Currency { Code = "EUR", Name = "Euro", Symbol = "€" },
                new Currency { Code = "GBP", Name = "British Pound", Symbol = "£" });
            await db.SaveChangesAsync();
        }

        if (await db.Companies.AnyAsync())
            return;

        var company = new Company
        {
            Name = "Demo Company Inc.",
            LegalName = "Demo Company Incorporated",
            BaseCurrencyCode = "USD",
            FiscalYearStartMonth = 1,
            Country = "USA"
        };
        db.Companies.Add(company);

        var fiscalYear = new FiscalYear
        {
            CompanyId = company.Id,
            Company = company,
            Name = $"FY{DateTime.UtcNow.Year}",
            StartDate = new DateTime(DateTime.UtcNow.Year, 1, 1),
            EndDate = new DateTime(DateTime.UtcNow.Year, 12, 31)
        };
        for (int m = 1; m <= 12; m++)
        {
            var start = new DateTime(DateTime.UtcNow.Year, m, 1);
            fiscalYear.Periods.Add(new FiscalPeriod
            {
                CompanyId = company.Id,
                Name = start.ToString("MMM yyyy"),
                PeriodNumber = m,
                StartDate = start,
                EndDate = start.AddMonths(1).AddDays(-1)
            });
        }
        db.FiscalYears.Add(fiscalYear);

        var accounts = BuildChartOfAccounts(company.Id);
        db.Accounts.AddRange(accounts);

        await db.SaveChangesAsync();

        const string adminEmail = "admin@demo.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Administrator",
                CompanyId = company.Id
            };
            var result = await userManager.CreateAsync(admin, "Admin@12345");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }

    private static List<Account> BuildChartOfAccounts(Guid companyId)
    {
        Account A(string code, string name, AccountType type, string? subType = null, string? key = null) => new()
        {
            CompanyId = companyId,
            Code = code,
            Name = name,
            Type = type,
            SubType = subType,
            IsSystemAccount = key != null,
            SystemAccountKey = key
        };

        return new List<Account>
        {
            A("1000", "Cash and Bank", AccountType.Asset, "Current Asset"),
            A("1100", "Accounts Receivable", AccountType.Asset, "Current Asset", SystemAccountKeys.AccountsReceivable),
            A("1200", "Inventory Asset", AccountType.Asset, "Current Asset"),
            A("1300", "Purchase Tax Receivable", AccountType.Asset, "Current Asset", SystemAccountKeys.PurchaseTaxReceivable),
            A("1500", "Fixed Assets", AccountType.Asset, "Fixed Asset"),
            A("1590", "Accumulated Depreciation", AccountType.Asset, "Fixed Asset"),

            A("2000", "Accounts Payable", AccountType.Liability, "Current Liability", SystemAccountKeys.AccountsPayable),
            A("2100", "Sales Tax Payable", AccountType.Liability, "Current Liability", SystemAccountKeys.SalesTaxPayable),

            A("3000", "Owner's Equity", AccountType.Equity, "Equity", SystemAccountKeys.OpeningBalanceEquity),
            A("3900", "Retained Earnings", AccountType.Equity, "Equity", SystemAccountKeys.RetainedEarnings),

            A("4000", "Sales Revenue", AccountType.Revenue, "Operating Revenue", SystemAccountKeys.DefaultSalesRevenue),

            A("5000", "Cost of Goods Sold", AccountType.Expense, "Cost of Sales", SystemAccountKeys.DefaultCogs),
            A("6000", "Rent Expense", AccountType.Expense, "Operating Expense"),
            A("6100", "Salaries and Wages", AccountType.Expense, "Operating Expense"),
            A("6200", "Utilities Expense", AccountType.Expense, "Operating Expense"),
            A("6300", "Office Supplies Expense", AccountType.Expense, "Operating Expense"),
            A("6400", "Depreciation Expense", AccountType.Expense, "Operating Expense"),
            A("6900", "Miscellaneous Expense", AccountType.Expense, "Operating Expense")
        };
    }
}
