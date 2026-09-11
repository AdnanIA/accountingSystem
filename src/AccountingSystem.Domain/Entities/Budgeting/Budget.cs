using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Organization;

namespace AccountingSystem.Domain.Entities.Budgeting;

public class Budget : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Name { get; set; } = null!;
    public Guid FiscalYearId { get; set; }
    public FiscalYear FiscalYear { get; set; } = null!;
    public bool IsActive { get; set; } = true;

    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}

public class BudgetLine : BaseEntity
{
    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public Guid FiscalPeriodId { get; set; }
    public FiscalPeriod FiscalPeriod { get; set; } = null!;

    public decimal Amount { get; set; }
}
