using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Purchases;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IVendorPaymentService
{
    Task<VendorPayment> RecordAndApplyAsync(Guid companyId, RecordVendorPaymentRequest request, string? postedBy, CancellationToken ct = default);
}

public class VendorPaymentService : IVendorPaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journal;
    private readonly INumberSequenceService _numbers;
    private readonly ISystemAccountResolver _systemAccounts;

    public VendorPaymentService(IApplicationDbContext db, IJournalEntryService journal, INumberSequenceService numbers, ISystemAccountResolver systemAccounts)
    {
        _db = db;
        _journal = journal;
        _numbers = numbers;
        _systemAccounts = systemAccounts;
    }

    public async Task<VendorPayment> RecordAndApplyAsync(Guid companyId, RecordVendorPaymentRequest request, string? postedBy, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            throw new DomainException("Payment amount must be positive.");

        var applyTotal = request.Applications?.Sum(a => a.Amount) ?? 0;
        if (applyTotal > request.Amount)
            throw new DomainException("Applied amount cannot exceed the payment amount.");

        var bankAccount = await _db.BankAccounts.FirstOrDefaultAsync(b => b.Id == request.BankAccountId && b.CompanyId == companyId, ct)
            ?? throw new DomainException("Bank account not found.");

        var paymentNumber = await _numbers.GetNextNumberAsync(companyId, "PAY", "PAY", ct);

        var payment = new VendorPayment
        {
            CompanyId = companyId,
            PaymentNumber = paymentNumber,
            VendorId = request.VendorId,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            Method = request.Method,
            ReferenceNumber = request.ReferenceNumber,
            BankAccountId = request.BankAccountId,
            Memo = request.Memo,
            UnappliedAmount = request.Amount - applyTotal
        };

        foreach (var app in request.Applications ?? new List<ApplyBillPaymentRequest>())
        {
            var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == app.BillId && b.CompanyId == companyId, ct)
                ?? throw new DomainException($"Bill {app.BillId} not found.");

            if (app.Amount > bill.Balance)
                throw new DomainException($"Applied amount {app.Amount} exceeds bill {bill.BillNumber} balance {bill.Balance}.");

            bill.AmountPaid += app.Amount;
            bill.Status = bill.Balance <= 0 ? BillStatus.Paid : BillStatus.PartiallyPaid;

            payment.Applications.Add(new VendorPaymentApplication { BillId = bill.Id, AmountApplied = app.Amount });
        }

        var apAccountId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.AccountsPayable, ct);
        var lines = new List<JournalEntryLine>
        {
            new() { AccountId = apAccountId, Debit = payment.Amount, Credit = 0, Description = $"Payment {paymentNumber}", VendorId = payment.VendorId },
            new() { AccountId = bankAccount.GLAccountId, Debit = 0, Credit = payment.Amount, Description = $"Payment {paymentNumber}", VendorId = payment.VendorId }
        };

        var entry = await _journal.CreateSystemEntryAsync(companyId, payment.PaymentDate, JournalSourceType.VendorPayment, payment.Id, $"Vendor payment {paymentNumber}", lines, postedBy, ct);
        payment.JournalEntryId = entry.Id;

        _db.VendorPayments.Add(payment);
        await _db.SaveChangesAsync(ct);
        return payment;
    }
}
