using AccountingSystem.Domain.Entities.Banking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.HasOne(b => b.GLAccount).WithMany().HasForeignKey(b => b.GLAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(b => b.Transactions).WithOne(t => t.BankAccount).HasForeignKey(t => t.BankAccountId);
    }
}

public class BankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.Property(t => t.Description).IsRequired().HasMaxLength(500);
        builder.HasOne(t => t.BankReconciliation)
            .WithMany(r => r.ReconciledTransactions)
            .HasForeignKey(t => t.BankReconciliationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> builder)
    {
        builder.HasOne(r => r.BankAccount).WithMany().HasForeignKey(r => r.BankAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(r => r.ClearedBalance);
        builder.Ignore(r => r.Difference);
    }
}
