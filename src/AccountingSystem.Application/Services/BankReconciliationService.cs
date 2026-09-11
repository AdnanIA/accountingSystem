using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Banking;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IBankReconciliationService
{
    Task<BankReconciliation> StartAsync(Guid companyId, Guid bankAccountId, DateTime statementDate, decimal statementBeginningBalance, decimal statementEndingBalance, CancellationToken ct = default);
    Task MarkTransactionClearedAsync(Guid reconciliationId, Guid bankTransactionId, CancellationToken ct = default);
    Task<BankReconciliation> CompleteAsync(Guid reconciliationId, string? completedBy, CancellationToken ct = default);
}

public class BankReconciliationService : IBankReconciliationService
{
    private readonly IApplicationDbContext _db;

    public BankReconciliationService(IApplicationDbContext db) => _db = db;

    public async Task<BankReconciliation> StartAsync(Guid companyId, Guid bankAccountId, DateTime statementDate, decimal statementBeginningBalance, decimal statementEndingBalance, CancellationToken ct = default)
    {
        var bankAccount = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == bankAccountId && b.CompanyId == companyId, ct)
            ?? throw new DomainException("Bank account not found.");

        var reconciliation = new BankReconciliation
        {
            BankAccountId = bankAccount.Id,
            StatementDate = statementDate,
            StatementBeginningBalance = statementBeginningBalance,
            StatementEndingBalance = statementEndingBalance
        };

        _db.BankReconciliations.Add(reconciliation);
        await _db.SaveChangesAsync(ct);
        return reconciliation;
    }

    public async Task MarkTransactionClearedAsync(Guid reconciliationId, Guid bankTransactionId, CancellationToken ct = default)
    {
        var reconciliation = await _db.BankReconciliations.FirstOrDefaultAsync(r => r.Id == reconciliationId, ct)
            ?? throw new DomainException("Reconciliation not found.");
        if (reconciliation.IsCompleted)
            throw new DomainException("Reconciliation is already completed.");

        var transaction = await _db.BankTransactions.FirstOrDefaultAsync(t => t.Id == bankTransactionId && t.BankAccountId == reconciliation.BankAccountId, ct)
            ?? throw new DomainException("Bank transaction not found.");

        transaction.IsReconciled = true;
        transaction.BankReconciliationId = reconciliation.Id;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BankReconciliation> CompleteAsync(Guid reconciliationId, string? completedBy, CancellationToken ct = default)
    {
        var reconciliation = await _db.BankReconciliations
            .Include(r => r.ReconciledTransactions)
            .FirstOrDefaultAsync(r => r.Id == reconciliationId, ct)
            ?? throw new DomainException("Reconciliation not found.");

        if (reconciliation.Difference != 0)
            throw new DomainException($"Cannot complete reconciliation: statement and cleared balances differ by {reconciliation.Difference}.");

        reconciliation.IsCompleted = true;
        reconciliation.CompletedAtUtc = DateTime.UtcNow;
        reconciliation.CompletedBy = completedBy;

        await _db.SaveChangesAsync(ct);
        return reconciliation;
    }
}
