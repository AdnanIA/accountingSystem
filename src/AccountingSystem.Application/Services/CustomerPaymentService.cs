using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Sales;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface ICustomerPaymentService
{
    Task<CustomerPayment> RecordAndApplyAsync(Guid companyId, RecordCustomerPaymentRequest request, string? postedBy, CancellationToken ct = default);
}

public class CustomerPaymentService : ICustomerPaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journal;
    private readonly INumberSequenceService _numbers;
    private readonly ISystemAccountResolver _systemAccounts;

    public CustomerPaymentService(IApplicationDbContext db, IJournalEntryService journal, INumberSequenceService numbers, ISystemAccountResolver systemAccounts)
    {
        _db = db;
        _journal = journal;
        _numbers = numbers;
        _systemAccounts = systemAccounts;
    }

    public async Task<CustomerPayment> RecordAndApplyAsync(Guid companyId, RecordCustomerPaymentRequest request, string? postedBy, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new DomainException("Payment amount must be positive.");

        var applyTotal = request.Applications?.Sum(a => a.Amount) ?? 0;
        if (applyTotal > request.Amount)
            throw new DomainException("Applied amount cannot exceed the payment amount.");

        var bankAccount = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.CompanyId == companyId, ct)
            ?? throw new DomainException("Bank account not found.");

        var paymentNumber = await _numbers.GetNextNumberAsync(companyId, "RCPT", "RCPT", ct);

        var payment = new CustomerPayment
        {
            CompanyId = companyId,
            PaymentNumber = paymentNumber,
            CustomerId = request.CustomerId,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            Method = request.Method,
            ReferenceNumber = request.ReferenceNumber,
            BankAccountId = request.BankAccountId,
            Memo = request.Memo,
            UnappliedAmount = request.Amount - applyTotal
        };

        foreach (var app in request.Applications ?? new List<ApplyPaymentRequest>())
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == app.InvoiceId && i.CompanyId == companyId, ct)
                ?? throw new DomainException($"Invoice {app.InvoiceId} not found.");

            if (app.Amount > invoice.Balance)
                throw new DomainException($"Applied amount {app.Amount} exceeds invoice {invoice.InvoiceNumber} balance {invoice.Balance}.");

            invoice.AmountPaid += app.Amount;
            invoice.Status = invoice.Balance <= 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

            payment.Applications.Add(new CustomerPaymentApplication { InvoiceId = invoice.Id, AmountApplied = app.Amount });
        }

        var arAccountId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.AccountsReceivable, ct);
        var lines = new List<JournalEntryLine>
        {
            new() { AccountId = bankAccount.GLAccountId, Debit = payment.Amount, Credit = 0, Description = $"Payment {paymentNumber}", CustomerId = payment.CustomerId },
            new() { AccountId = arAccountId, Debit = 0, Credit = payment.Amount, Description = $"Payment {paymentNumber}", CustomerId = payment.CustomerId }
        };

        var entry = await _journal.CreateSystemEntryAsync(companyId, payment.PaymentDate, JournalSourceType.CustomerPayment, payment.Id, $"Customer payment {paymentNumber}", lines, postedBy, ct);
        payment.JournalEntryId = entry.Id;

        _db.CustomerPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment;
    }
}
