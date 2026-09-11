using AccountingSystem.Domain.Common;

namespace AccountingSystem.Domain.Entities.Organization;

public class Company : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? LegalName { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string BaseCurrencyCode { get; set; } = "USD";
    public int FiscalYearStartMonth { get; set; } = 1;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FiscalYear> FiscalYears { get; set; } = new List<FiscalYear>();
}
