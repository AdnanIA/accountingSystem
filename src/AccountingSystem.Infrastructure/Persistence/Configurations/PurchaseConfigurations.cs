using AccountingSystem.Domain.Entities.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.Property(b => b.BillNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(b => new { b.CompanyId, b.BillNumber }).IsUnique();
        builder.HasOne(b => b.Vendor).WithMany().HasForeignKey(b => b.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(b => b.Lines).WithOne(l => l.Bill).HasForeignKey(l => l.BillId);
        builder.Ignore(b => b.Balance);
    }
}

public class BillLineConfiguration : IEntityTypeConfiguration<BillLine>
{
    public void Configure(EntityTypeBuilder<BillLine> builder)
    {
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.TaxCode).WithMany().HasForeignKey(l => l.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(l => l.LineSubTotal);
        builder.Ignore(l => l.LineTotal);
    }
}

public class VendorPaymentConfiguration : IEntityTypeConfiguration<VendorPayment>
{
    public void Configure(EntityTypeBuilder<VendorPayment> builder)
    {
        builder.Property(p => p.PaymentNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => new { p.CompanyId, p.PaymentNumber }).IsUnique();
        builder.HasOne(p => p.Vendor).WithMany().HasForeignKey(p => p.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.BankAccount).WithMany().HasForeignKey(p => p.BankAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Applications).WithOne(a => a.VendorPayment).HasForeignKey(a => a.VendorPaymentId);
    }
}

public class VendorPaymentApplicationConfiguration : IEntityTypeConfiguration<VendorPaymentApplication>
{
    public void Configure(EntityTypeBuilder<VendorPaymentApplication> builder)
    {
        builder.HasOne(a => a.Bill).WithMany(b => b.PaymentApplications).HasForeignKey(a => a.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VendorCreditConfiguration : IEntityTypeConfiguration<VendorCredit>
{
    public void Configure(EntityTypeBuilder<VendorCredit> builder)
    {
        builder.Property(c => c.VendorCreditNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(c => new { c.CompanyId, c.VendorCreditNumber }).IsUnique();
        builder.HasOne(c => c.Vendor).WithMany().HasForeignKey(c => c.VendorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Applications).WithOne(a => a.VendorCredit).HasForeignKey(a => a.VendorCreditId);
    }
}

public class VendorCreditApplicationConfiguration : IEntityTypeConfiguration<VendorCreditApplication>
{
    public void Configure(EntityTypeBuilder<VendorCreditApplication> builder)
    {
        builder.HasOne(a => a.Bill).WithMany().HasForeignKey(a => a.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}
