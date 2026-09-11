using AccountingSystem.Domain.Common;

namespace AccountingSystem.Domain.Entities.Currency;

public class Currency
{
    /// <summary>ISO 4217 code, e.g. USD, EUR. Used as the primary key.</summary>
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class ExchangeRate : BaseEntity
{
    public string CurrencyCode { get; set; } = null!;
    public DateTime RateDate { get; set; }
    /// <summary>Units of the company's base currency per 1 unit of CurrencyCode.</summary>
    public decimal RateToBase { get; set; }
}
