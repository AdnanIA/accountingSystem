using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class FiscalPeriodsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IPeriodClosingService _closingService;

    public FiscalPeriodsController(IApplicationDbContext db, IPeriodClosingService closingService)
    {
        _db = db;
        _closingService = closingService;
    }

    [HttpGet("years")]
    public async Task<ActionResult<List<FiscalYear>>> GetYears(CancellationToken ct)
        => Ok(await _db.FiscalYears.Where(y => y.CompanyId == CompanyId).OrderBy(y => y.StartDate).ToListAsync(ct));

    [HttpGet("years/{yearId:guid}/periods")]
    public async Task<ActionResult<List<FiscalPeriod>>> GetPeriods(Guid yearId, CancellationToken ct)
        => Ok(await _db.FiscalPeriods.Where(p => p.FiscalYearId == yearId && p.CompanyId == CompanyId).OrderBy(p => p.PeriodNumber).ToListAsync(ct));

    [HttpPost("periods/{periodId:guid}/close")]
    public async Task<IActionResult> ClosePeriod(Guid periodId, CancellationToken ct)
    {
        await _closingService.CloseFiscalPeriodAsync(CompanyId, periodId, CurrentUserName, ct);
        return NoContent();
    }

    [HttpPost("periods/{periodId:guid}/reopen")]
    public async Task<IActionResult> ReopenPeriod(Guid periodId, CancellationToken ct)
    {
        await _closingService.ReopenFiscalPeriodAsync(CompanyId, periodId, CurrentUserName, ct);
        return NoContent();
    }

    [HttpPost("years/{yearId:guid}/close")]
    public async Task<IActionResult> CloseYear(Guid yearId, CancellationToken ct)
    {
        await _closingService.CloseFiscalYearAsync(CompanyId, yearId, CurrentUserName, ct);
        return NoContent();
    }
}
