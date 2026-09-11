using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Api.Controllers;

public class ReportsController : ApiControllerBase
{
    private readonly IReportingService _reports;

    public ReportsController(IReportingService reports) => _reports = reports;

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReport>> TrialBalance([FromQuery] DateTime asOfDate, CancellationToken ct)
        => Ok(await _reports.GetTrialBalanceAsync(CompanyId, asOfDate, ct));

    [HttpGet("income-statement")]
    public async Task<ActionResult<IncomeStatementReport>> IncomeStatement([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
        => Ok(await _reports.GetIncomeStatementAsync(CompanyId, startDate, endDate, ct));

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetReport>> BalanceSheet([FromQuery] DateTime asOfDate, CancellationToken ct)
        => Ok(await _reports.GetBalanceSheetAsync(CompanyId, asOfDate, ct));

    [HttpGet("general-ledger/{accountId:guid}")]
    public async Task<ActionResult<GeneralLedgerReport>> GeneralLedger(Guid accountId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
        => Ok(await _reports.GetGeneralLedgerAsync(CompanyId, accountId, startDate, endDate, ct));

    [HttpGet("ar-aging")]
    public async Task<ActionResult<AgingReport>> ArAging([FromQuery] DateTime asOfDate, CancellationToken ct)
        => Ok(await _reports.GetArAgingAsync(CompanyId, asOfDate, ct));

    [HttpGet("ap-aging")]
    public async Task<ActionResult<AgingReport>> ApAging([FromQuery] DateTime asOfDate, CancellationToken ct)
        => Ok(await _reports.GetApAgingAsync(CompanyId, asOfDate, ct));

    [HttpGet("cash-flow")]
    public async Task<ActionResult<CashFlowReport>> CashFlow([FromQuery] DateTime startDate, [FromQuery] DateTime endDate, CancellationToken ct)
        => Ok(await _reports.GetCashFlowAsync(CompanyId, startDate, endDate, ct));
}
