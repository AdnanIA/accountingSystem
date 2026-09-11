using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Budgeting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record BudgetLineRequest(Guid AccountId, Guid FiscalPeriodId, decimal Amount);
public record CreateBudgetRequest(string Name, Guid FiscalYearId, List<BudgetLineRequest> Lines);

public class BudgetsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public BudgetsController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Budget>>> GetAll(CancellationToken ct)
        => Ok(await _db.Budgets.Where(b => b.CompanyId == CompanyId).Include(b => b.Lines).ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<Budget>> Create(CreateBudgetRequest request, CancellationToken ct)
    {
        var budget = new Budget
        {
            CompanyId = CompanyId,
            Name = request.Name,
            FiscalYearId = request.FiscalYearId,
            Lines = request.Lines.Select(l => new BudgetLine { AccountId = l.AccountId, FiscalPeriodId = l.FiscalPeriodId, Amount = l.Amount }).ToList()
        };
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync(ct);
        return Ok(budget);
    }
}
