using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public bool IsActive { get; set; } = true;
}
