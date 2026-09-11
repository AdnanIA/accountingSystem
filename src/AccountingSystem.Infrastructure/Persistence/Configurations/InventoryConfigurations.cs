using AccountingSystem.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.Property(i => i.SKU).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(i => new { i.CompanyId, i.SKU }).IsUnique();

        builder.HasOne(i => i.IncomeAccount).WithMany().HasForeignKey(i => i.IncomeAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.ExpenseAccount).WithMany().HasForeignKey(i => i.ExpenseAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.InventoryAssetAccount).WithMany().HasForeignKey(i => i.InventoryAssetAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.DefaultTaxCode).WithMany().HasForeignKey(i => i.DefaultTaxCodeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.StockTransactions).WithOne(s => s.Item).HasForeignKey(s => s.ItemId);
    }
}

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.Property(s => s.ReferenceType).HasMaxLength(50);
        builder.Ignore(s => s.TotalCost);
    }
}
