using AccountingSystem.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingSystem.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(i => new { i.CompanyId, i.InvoiceNumber }).IsUnique();
        builder.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(i => i.Lines).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId);
        builder.Ignore(i => i.Balance);
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(l => l.TaxCode).WithMany().HasForeignKey(l => l.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(l => l.LineSubTotal);
        builder.Ignore(l => l.LineTotal);
    }
}

public class CustomerPaymentConfiguration : IEntityTypeConfiguration<CustomerPayment>
{
    public void Configure(EntityTypeBuilder<CustomerPayment> builder)
    {
        builder.Property(p => p.PaymentNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => new { p.CompanyId, p.PaymentNumber }).IsUnique();
        builder.HasOne(p => p.Customer).WithMany().HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.BankAccount).WithMany().HasForeignKey(p => p.BankAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(p => p.Applications).WithOne(a => a.CustomerPayment).HasForeignKey(a => a.CustomerPaymentId);
    }
}

public class CustomerPaymentApplicationConfiguration : IEntityTypeConfiguration<CustomerPaymentApplication>
{
    public void Configure(EntityTypeBuilder<CustomerPaymentApplication> builder)
    {
        builder.HasOne(a => a.Invoice).WithMany(i => i.PaymentApplications).HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CreditMemoConfiguration : IEntityTypeConfiguration<CreditMemo>
{
    public void Configure(EntityTypeBuilder<CreditMemo> builder)
    {
        builder.Property(c => c.CreditMemoNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(c => new { c.CompanyId, c.CreditMemoNumber }).IsUnique();
        builder.HasOne(c => c.Customer).WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Applications).WithOne(a => a.CreditMemo).HasForeignKey(a => a.CreditMemoId);
    }
}

public class CreditMemoApplicationConfiguration : IEntityTypeConfiguration<CreditMemoApplication>
{
    public void Configure(EntityTypeBuilder<CreditMemoApplication> builder)
    {
        builder.HasOne(a => a.Invoice).WithMany().HasForeignKey(a => a.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
