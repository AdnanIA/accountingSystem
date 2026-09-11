using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class AccountsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IAccountService _accountService;

    public AccountsController(IApplicationDbContext db, IAccountService accountService)
    {
        _db = db;
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Accounts.Where(a => a.CompanyId == CompanyId);
        if (!includeInactive) query = query.Where(a => a.IsActive);

        var accounts = await query.OrderBy(a => a.Code)
            .Select(a => new AccountDto(a.Id, a.Code, a.Name, a.Type, a.SubType, a.ParentAccountId, a.IsActive, a.NormalBalance))
            .ToListAsync(ct);

        return Ok(accounts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetById(Guid id, CancellationToken ct)
    {
        var a = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId, ct);
        if (a is null) return NotFound();
        return Ok(new AccountDto(a.Id, a.Code, a.Name, a.Type, a.SubType, a.ParentAccountId, a.IsActive, a.NormalBalance));
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create(CreateAccountRequest request, CancellationToken ct)
    {
        var account = await _accountService.CreateAsync(CompanyId, request, ct);
        var dto = new AccountDto(account.Id, account.Code, account.Name, account.Type, account.SubType, account.ParentAccountId, account.IsActive, account.NormalBalance);
        return CreatedAtAction(nameof(GetById), new { id = account.Id }, dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _accountService.DeactivateAsync(CompanyId, id, ct);
        return NoContent();
    }
}
