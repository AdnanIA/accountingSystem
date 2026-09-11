namespace AccountingSystem.Domain.Entities.Organization;

/// <summary>Tracks the next document number per company and document type (e.g. "INV", "BILL", "JE").</summary>
public class NumberSequence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string DocumentType { get; set; } = null!;
    public string Prefix { get; set; } = null!;
    public int NextNumber { get; set; } = 1;
    public int PaddingLength { get; set; } = 6;

    public string Format() => $"{Prefix}-{NextNumber.ToString().PadLeft(PaddingLength, '0')}";
}
