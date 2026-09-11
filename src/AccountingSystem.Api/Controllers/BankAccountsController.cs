using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Banking;
using AccountingSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record BankAccountRequest(string Name, string? BankName, string? AccountNumberMasked, Guid GLAccountId, string CurrencyCode, decimal OpeningBalance, DateTime OpeningBalanceDate);
public record BankTransactionRequest(DateTime TransactionDate, string Description, BankTransactionType Type, decimal Amount, string? ReferenceNumber);
public record StartReconciliationRequest(DateTime StatementDate, decimal StatementBeginningBalance, decimal StatementEndingBalance);

public class BankAccountsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IBankReconciliationService _reconciliationService;

    public BankAccountsController(IApplicationDbContext db, IBankReconciliationService reconciliationService)
    {
        _db = db;
        _reconciliationService = reconciliationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BankAccount>>> GetAll(CancellationToken ct)
        => Ok(await _db.BankAccounts.Where(b => b.CompanyId == CompanyId && b.IsActive).ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<BankAccount>> Create(BankAccountRequest request, CancellationToken ct)
    {
        var account = new BankAccount
        {
            CompanyId = CompanyId,
            Name = request.Name,
            BankName = request.BankName,
            AccountNumberMasked = request.AccountNumberMasked,
            GLAccountId = request.GLAccountId,
            CurrencyCode = request.CurrencyCode,
            OpeningBalance = request.OpeningBalance,
            OpeningBalanceDate = request.OpeningBalanceDate
        };
        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return Ok(account);
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<ActionResult<List<BankTransaction>>> GetTransactions(Guid id, CancellationToken ct)
        => Ok(await _db.BankTransactions.Where(t => t.BankAccountId == id).OrderByDescending(t => t.TransactionDate).Take(500).ToListAsync(ct));

    [HttpPost("{id:guid}/transactions")]
    public async Task<ActionResult<BankTransaction>> AddTransaction(Guid id, BankTransactionRequest request, CancellationToken ct)
    {
        var bankAccount = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == CompanyId, ct);
        if (bankAccount is null) return NotFound();

        var transaction = new BankTransaction
        {
            BankAccountId = id,
            TransactionDate = request.TransactionDate,
            Description = request.Description,
            Type = request.Type,
            Amount = request.Amount,
            ReferenceNumber = request.ReferenceNumber
        };
        _db.BankTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
        return Ok(transaction);
    }

    [HttpPost("{id:guid}/reconciliations")]
    public async Task<ActionResult<BankReconciliation>> StartReconciliation(Guid id, StartReconciliationRequest request, CancellationToken ct)
    {
        var reconciliation = await _reconciliationService.StartAsync(CompanyId, id, request.StatementDate, request.StatementBeginningBalance, request.StatementEndingBalance, ct);
        return Ok(reconciliation);
    }

    [HttpPost("reconciliations/{reconciliationId:guid}/transactions/{transactionId:guid}/clear")]
    public async Task<IActionResult> MarkCleared(Guid reconciliationId, Guid transactionId, CancellationToken ct)
    {
        await _reconciliationService.MarkTransactionClearedAsync(reconciliationId, transactionId, ct);
        return NoContent();
    }

    [HttpPost("reconciliations/{reconciliationId:guid}/complete")]
    public async Task<ActionResult<BankReconciliation>> CompleteReconciliation(Guid reconciliationId, CancellationToken ct)
    {
        var reconciliation = await _reconciliationService.CompleteAsync(reconciliationId, CurrentUserName, ct);
        return Ok(reconciliation);
    }
}
