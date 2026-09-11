using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Assets;

public class FixedAsset : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal SalvageValue { get; set; }
    public int UsefulLifeMonths { get; set; }
    public DepreciationMethod Method { get; set; } = DepreciationMethod.StraightLine;
    public decimal? DecliningBalanceRatePercent { get; set; }

    public Guid AssetAccountId { get; set; }
    public Account AssetAccount { get; set; } = null!;
    public Guid AccumulatedDepreciationAccountId { get; set; }
    public Account AccumulatedDepreciationAccount { get; set; } = null!;
    public Guid DepreciationExpenseAccountId { get; set; }
    public Account DepreciationExpenseAccount { get; set; } = null!;

    public AssetStatus Status { get; set; } = AssetStatus.Active;
    public DateTime? DisposalDate { get; set; }
    public decimal? DisposalProceeds { get; set; }

    public decimal AccumulatedDepreciation { get; set; }
    public decimal NetBookValue => AcquisitionCost - AccumulatedDepreciation;

    public ICollection<DepreciationEntry> DepreciationEntries { get; set; } = new List<DepreciationEntry>();
}

public class DepreciationEntry : BaseEntity
{
    public Guid FixedAssetId { get; set; }
    public FixedAsset FixedAsset { get; set; } = null!;

    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
    public Guid? JournalEntryId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
