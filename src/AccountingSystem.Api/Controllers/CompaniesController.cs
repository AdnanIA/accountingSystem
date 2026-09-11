using AccountingSystem.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class CompaniesController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public CompaniesController(IApplicationDbContext db) => _db = db;

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(c => c.Id == CompanyId, ct);
        return company is null ? NotFound() : Ok(company);
    }
}
