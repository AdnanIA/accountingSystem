using AccountingSystem.Domain.Common;

namespace AccountingSystem.Domain.Entities.Parties;

public class Customer : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? BillingAddressLine1 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }

    public int PaymentTermsDays { get; set; } = 30;
    public decimal CreditLimit { get; set; }
    public bool TaxExempt { get; set; }
    public Guid? DefaultTaxCodeId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
