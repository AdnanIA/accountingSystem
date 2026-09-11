using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Tax;

public class TaxCode : AuditableEntity, ICompanyOwned
{
    public Guid CompanyId { get; set; }

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal RatePercent { get; set; }
    public TaxType Type { get; set; }

    public Guid TaxPayableOrReceivableAccountId { get; set; }
    public Account TaxAccount { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
