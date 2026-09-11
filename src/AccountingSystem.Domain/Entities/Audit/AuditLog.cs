using AccountingSystem.Domain.Common;
using AccountingSystem.Domain.Enums;

namespace AccountingSystem.Domain.Entities.Audit;

public class AuditLog : BaseEntity
{
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    public string EntityName { get; set; } = null!;
    public Guid? EntityId { get; set; }
    public AuditAction Action { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    /// <summary>JSON-serialized before/after values for Update actions.</summary>
    public string? Changes { get; set; }
    public string? IpAddress { get; set; }
}
