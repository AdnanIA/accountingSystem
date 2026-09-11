using AccountingSystem.Domain.Entities.Parties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Code).IsRequired().HasMaxLength(30);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => new { c.CompanyId, c.Code }).IsUnique();
    }
}

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.Property(v => v.Code).IsRequired().HasMaxLength(30);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(v => new { v.CompanyId, v.Code }).IsUnique();
    }
}
