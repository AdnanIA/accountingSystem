using AccountingSystem.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.Property(a => a.Code).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(a => new { a.CompanyId, a.Code }).IsUnique();
        builder.HasIndex(a => new { a.CompanyId, a.SystemAccountKey });

        builder.HasOne(a => a.ParentAccount)
            .WithMany(a => a.ChildAccounts)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(a => a.NormalBalance);
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.Property(e => e.EntryNumber).IsRequired().HasMaxLength(30);
        builder.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.HasIndex(e => new { e.CompanyId, e.EntryNumber }).IsUnique();
        builder.HasIndex(e => new { e.CompanyId, e.EntryDate });

        builder.HasOne(e => e.FiscalPeriod).WithMany().HasForeignKey(e => e.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReversalOfEntry)
            .WithMany()
            .HasForeignKey(e => e.ReversalOfEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Lines).WithOne(l => l.JournalEntry).HasForeignKey(l => l.JournalEntryId).OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.TotalDebit);
        builder.Ignore(e => e.TotalCredit);
        builder.Ignore(e => e.IsBalanced);
    }
}

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.HasOne(l => l.Account).WithMany(a => a.JournalEntryLines).HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.AccountId);
    }
}
