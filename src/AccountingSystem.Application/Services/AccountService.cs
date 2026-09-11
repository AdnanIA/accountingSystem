using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IAccountService
{
    Task<Account> CreateAsync(Guid companyId, CreateAccountRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid companyId, Guid accountId, CancellationToken ct = default);
}

public class AccountService : IAccountService
{
    private readonly IApplicationDbContext _db;

    public AccountService(IApplicationDbContext db) => _db = db;

    public async Task<Account> CreateAsync(Guid companyId, CreateAccountRequest request, CancellationToken ct = default)
    {
        var codeExists = await _db.Accounts.AnyAsync(a => a.CompanyId == companyId && a.Code == request.Code, ct);
        if (codeExists)
            throw new DomainException($"Account code '{request.Code}' already exists.");

        if (request.ParentAccountId.HasValue)
        {
            var parent = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == request.ParentAccountId && a.CompanyId == companyId, ct)
                ?? throw new DomainException("Parent account not found.");
            if (parent.Type != request.Type)
                throw new DomainException("A sub-account must have the same account type as its parent.");
        }

        var account = new Account
        {
            CompanyId = companyId,
            Code = request.Code,
            Name = request.Name,
            Type = request.Type,
            SubType = request.SubType,
            ParentAccountId = request.ParentAccountId,
            Description = request.Description
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
        return account;
    }

    public async Task DeactivateAsync(Guid companyId, Guid accountId, CancellationToken ct = default)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.CompanyId == companyId, ct)
            ?? throw new DomainException("Account not found.");

        if (account.IsSystemAccount)
            throw new DomainException("System accounts cannot be deactivated.");

        var hasActivity = await _db.JournalEntryLines.AnyAsync(l => l.AccountId == accountId, ct);
        if (hasActivity)
        {
            account.IsActive = false;
        }
        else
        {
            _db.Accounts.Remove(account);
        }

        await _db.SaveChangesAsync(ct);
    }
}
