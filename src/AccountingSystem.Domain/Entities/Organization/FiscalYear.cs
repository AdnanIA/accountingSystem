using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Organization;

public class FiscalYear : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }

    public ICollection<FiscalPeriod> Periods { get; set; } = new List<FiscalPeriod>();
}

public class FiscalPeriod : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public Guid FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = null!;

    public string Name { get; set; } = null!;
    public int PeriodNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public FiscalPeriodStatus Status { get; set; } = FiscalPeriodStatus.Open;
    public DateTime? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }
}
