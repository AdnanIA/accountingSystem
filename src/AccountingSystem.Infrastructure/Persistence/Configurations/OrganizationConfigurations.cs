using AccountingSystem.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.BaseCurrencyCode).IsRequired().HasMaxLength(3);
    }
}

public class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.Property(f => f.Name).IsRequired().HasMaxLength(50);
        builder.HasOne(f => f.Company).WithMany(c => c.FiscalYears).HasForeignKey(f => f.CompanyId);
        builder.HasIndex(f => new { f.CompanyId, f.Name }).IsUnique();
    }
}

public class FiscalPeriodConfiguration : IEntityTypeConfiguration<FiscalPeriod>
{
    public void Configure(EntityTypeBuilder<FiscalPeriod> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(50);
        builder.HasOne(p => p.FiscalYear).WithMany(y => y.Periods).HasForeignKey(p => p.FiscalYearId);
        builder.HasIndex(p => new { p.CompanyId, p.StartDate, p.EndDate });
    }
}

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.Property(s => s.DocumentType).IsRequired().HasMaxLength(30);
        builder.Property(s => s.Prefix).IsRequired().HasMaxLength(20);
        builder.HasIndex(s => new { s.CompanyId, s.DocumentType }).IsUnique();
    }
}
