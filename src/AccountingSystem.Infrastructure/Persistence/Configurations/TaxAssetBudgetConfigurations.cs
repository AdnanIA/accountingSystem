using AccountingSystem.Domain.Entities.Assets;
using AccountingSystem.Domain.Entities.Budgeting;
using AccountingSystem.Domain.Entities.Tax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class TaxCodeConfiguration : IEntityTypeConfiguration<TaxCode>
{
    public void Configure(EntityTypeBuilder<TaxCode> builder)
    {
        builder.Property(t => t.Code).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => new { t.CompanyId, t.Code }).IsUnique();
        builder.HasOne(t => t.TaxAccount).WithMany().HasForeignKey(t => t.TaxPayableOrReceivableAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.Property(a => a.Code).IsRequired().HasMaxLength(30);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(a => new { a.CompanyId, a.Code }).IsUnique();

        builder.HasOne(a => a.AssetAccount).WithMany().HasForeignKey(a => a.AssetAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AccumulatedDepreciationAccount).WithMany().HasForeignKey(a => a.AccumulatedDepreciationAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.DepreciationExpenseAccount).WithMany().HasForeignKey(a => a.DepreciationExpenseAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.DepreciationEntries).WithOne(d => d.FixedAsset).HasForeignKey(d => d.FixedAssetId);
        builder.Ignore(a => a.NetBookValue);
    }
}

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.HasOne(b => b.FiscalYear).WithMany().HasForeignKey(b => b.FiscalYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(b => b.Lines).WithOne(l => l.Budget).HasForeignKey(l => l.BudgetId);
    }
}

public class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.HasOne(l => l.Account).WithMany().HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.FiscalPeriod).WithMany().HasForeignKey(l => l.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => new { l.BudgetId, l.AccountId, l.FiscalPeriodId }).IsUnique();
    }
}
