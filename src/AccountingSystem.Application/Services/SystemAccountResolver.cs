using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

/// <summary>Well-known keys used to tag Chart of Accounts entries that the posting engine relies on.</summary>
public static class SystemAccountKeys
{
    public const string AccountsReceivable = "AR";
    public const string AccountsPayable = "AP";
    public const string SalesTaxPayable = "SALES_TAX_PAYABLE";
    public const string PurchaseTaxReceivable = "PURCHASE_TAX_RECEIVABLE";
    public const string RetainedEarnings = "RETAINED_EARNINGS";
    public const string OpeningBalanceEquity = "OPENING_BALANCE_EQUITY";
    public const string DefaultCogs = "DEFAULT_COGS";
    public const string DefaultSalesRevenue = "DEFAULT_SALES_REVENUE";
}

public interface ISystemAccountResolver
{
    Task<Guid> ResolveAsync(Guid companyId, string systemAccountKey, CancellationToken ct = default);
}

public class SystemAccountResolver : ISystemAccountResolver
{
    private readonly IApplicationDbContext _db;

    public SystemAccountResolver(IApplicationDbContext db) => _db = db;

    public async Task<Guid> ResolveAsync(Guid companyId, string systemAccountKey, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(
            a => a.CompanyId == companyId && a.SystemAccountKey == systemAccountKey, ct);

        return account?.Id
            ?? throw new DomainException($"Chart of Accounts is missing a required system account '{systemAccountKey}'. Configure it before posting transactions.");
    }
}
