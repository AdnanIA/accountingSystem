using AccountingSystem.Domain.Common;

namespace AccountingSystem.Domain.Entities.Parties;

public class Vendor : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    public int PaymentTermsDays { get; set; } = 30;
    public Guid? DefaultTaxCodeId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool Is1099Vendor { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}
