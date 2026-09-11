using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Assets;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IFixedAssetService
{
    /// <summary>Runs one month of depreciation for every active asset as of the given period-end date and posts a single journal entry.</summary>
    Task<int> RunMonthlyDepreciationAsync(Guid companyId, DateTime periodEndDate, string? postedBy, CancellationToken ct = default);

    Task DisposeAssetAsync(Guid companyId, Guid assetId, DateTime disposalDate, decimal proceeds, string? postedBy, CancellationToken ct = default);
}

public class FixedAssetService : IFixedAssetService
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journal;

    public FixedAssetService(IApplicationDbContext db, IJournalEntryService journal)
    {
        _db = db;
        _journal = journal;
    }

    public async Task<int> RunMonthlyDepreciationAsync(Guid companyId, DateTime periodEndDate, string? postedBy, CancellationToken ct = default)
    {
        var assets = await _db.FixedAssets
            .Where(a => a.CompanyId == companyId && a.Status == AssetStatus.Active && a.AcquisitionDate <= periodEndDate)
            .ToListAsync(ct);

        var lines = new List<JournalEntryLine>();
        int count = 0;

        foreach (var asset in assets)
        {
            if (asset.UsefulLifeMonths <= 0) continue;

            var monthlyAmount = asset.Method == DepreciationMethod.StraightLine
                ? Math.Round((asset.AcquisitionCost - asset.SalvageValue) / asset.UsefulLifeMonths, 2)
                : Math.Round((asset.AcquisitionCost - asset.AccumulatedDepreciation) * (asset.DecliningBalanceRatePercent ?? 0) / 100m / 12m, 2);

            var remaining = asset.AcquisitionCost - asset.SalvageValue - asset.AccumulatedDepreciation;
            if (remaining <= 0) continue;

            var amount = Math.Min(monthlyAmount, remaining);
            if (amount <= 0) continue;

            asset.AccumulatedDepreciation += amount;
            if (asset.AcquisitionCost - asset.AccumulatedDepreciation <= asset.SalvageValue)
                asset.Status = AssetStatus.FullyDepreciated;

            asset.DepreciationEntries.Add(new DepreciationEntry { PeriodDate = periodEndDate, Amount = amount });

            lines.Add(new JournalEntryLine { AccountId = asset.DepreciationExpenseAccountId, Debit = amount, Credit = 0, Description = $"Depreciation - {asset.Name}" });
            lines.Add(new JournalEntryLine { AccountId = asset.AccumulatedDepreciationAccountId, Debit = 0, Credit = amount, Description = $"Depreciation - {asset.Name}" });
            count++;
        }

        if (lines.Count > 0)
        {
            var entry = await _journal.CreateSystemEntryAsync(companyId, periodEndDate, JournalSourceType.Depreciation, Guid.Empty, $"Monthly depreciation {periodEndDate:yyyy-MM}", lines, postedBy, ct);
            foreach (var asset in assets.Where(a => a.DepreciationEntries.Any(d => d.PeriodDate == periodEndDate)))
                foreach (var de in asset.DepreciationEntries.Where(d => d.PeriodDate == periodEndDate && d.JournalEntryId == null))
                    de.JournalEntryId = entry.Id;
        }

        await _db.SaveChangesAsync(ct);
        return count;
    }

    public async Task DisposeAssetAsync(Guid companyId, Guid assetId, DateTime disposalDate, decimal proceeds, string? postedBy, CancellationToken ct = default)
    {
        var asset = await _db.FixedAssets.FirstOrDefaultAsync(a => a.Id == assetId && a.CompanyId == companyId, ct)
            ?? throw new DomainException("Fixed asset not found.");

        if (asset.Status == AssetStatus.Disposed)
            throw new DomainException("Asset has already been disposed.");

        asset.Status = AssetStatus.Disposed;
        asset.DisposalDate = disposalDate;
        asset.DisposalProceeds = proceeds;

        await _db.SaveChangesAsync(ct);
    }
}
