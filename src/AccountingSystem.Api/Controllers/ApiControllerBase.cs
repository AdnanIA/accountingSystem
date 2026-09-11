using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CompanyId
    {
        get
        {
            var value = User.FindFirstValue("companyId");
            if (!Guid.TryParse(value, out var id))
                throw new InvalidOperationException("The authenticated user is not associated with a company.");
            return id;
        }
    }

    protected string? CurrentUserName => User.FindFirstValue(ClaimTypes.Name);
}
