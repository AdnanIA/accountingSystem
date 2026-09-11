using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Entities.Purchases;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IBillService
{
    Task<Bill> CreateDraftAsync(Guid companyId, CreateBillRequest request, CancellationToken ct = default);
    Task<Bill> PostAsync(Guid companyId, Guid billId, string? postedBy, CancellationToken ct = default);
    Task VoidAsync(Guid companyId, Guid billId, string? voidedBy, CancellationToken ct = default);
}

public class BillService : IBillService
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journal;
    private readonly INumberSequenceService _numbers;
    private readonly ISystemAccountResolver _systemAccounts;
    private readonly IInventoryService _inventory;

    public BillService(IApplicationDbContext db, IJournalEntryService journal, INumberSequenceService numbers, ISystemAccountResolver systemAccounts, IInventoryService inventory)
    {
        _db = db;
        _journal = journal;
        _numbers = numbers;
        _systemAccounts = systemAccounts;
        _inventory = inventory;
    }

    public async Task<Bill> CreateDraftAsync(Guid companyId, CreateBillRequest request, CancellationToken ct = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new DomainException("A bill requires at least one line.");

        var taxCodeIds = request.Lines.Where(l => l.TaxCodeId.HasValue).Select(l => l.TaxCodeId!.Value).Distinct().ToList();
        var taxCodes = await _db.TaxCodes.Where(t => taxCodeIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);

        var billNumber = await _numbers.GetNextNumberAsync(companyId, "BILL", "BILL", ct);

        var bill = new Bill
        {
            CompanyId = companyId,
            BillNumber = billNumber,
            VendorInvoiceNumber = request.VendorInvoiceNumber,
            VendorId = request.VendorId,
            BillDate = request.BillDate,
            DueDate = request.DueDate,
            Status = BillStatus.Draft,
            CurrencyCode = request.CurrencyCode,
            ExchangeRateToBase = request.ExchangeRateToBase,
            Memo = request.Memo
        };

        int lineNo = 1;
        foreach (var l in request.Lines)
        {
            var line = new BillLine
            {
                LineNumber = lineNo++,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                TaxCodeId = l.TaxCodeId,
                ExpenseAccountId = l.ExpenseAccountId
            };

            var subTotal = Math.Round(l.Quantity * l.UnitCost, 2);
            if (l.TaxCodeId.HasValue && taxCodes.TryGetValue(l.TaxCodeId.Value, out var taxCode))
                line.TaxAmount = Math.Round(subTotal * taxCode.RatePercent / 100m, 2);

            bill.Lines.Add(line);
        }

        bill.SubTotal = bill.Lines.Sum(l => l.LineSubTotal);
        bill.TaxTotal = bill.Lines.Sum(l => l.TaxAmount);
        bill.Total = bill.SubTotal + bill.TaxTotal;

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync(ct);
        return bill;
    }

    public async Task<Bill> PostAsync(Guid companyId, Guid billId, string? postedBy, CancellationToken ct = default)
    {
        var bill = await _db.Bills
            .Include(b => b.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(b => b.Id == billId && b.CompanyId == companyId, ct)
            ?? throw new DomainException("Bill not found.");

        if (bill.Status != BillStatus.Draft)
            throw new DomainException("Only draft bills can be posted.");

        var apAccountId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.AccountsPayable, ct);
        var defaultExpenseId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.DefaultCogs, ct);

        var lines = new List<JournalEntryLine>();

        foreach (var line in bill.Lines)
        {
            if (line.Item is { Type: ItemType.Inventory } item)
            {
                var newAvgCost = await _inventory.ReceiveStockAsync(item, bill.BillDate, line.Quantity, line.UnitCost, nameof(Bill), bill.Id, ct);
                var inventoryAccountId = item.InventoryAssetAccountId ?? line.ExpenseAccountId ?? defaultExpenseId;
                lines.Add(new JournalEntryLine { AccountId = inventoryAccountId, Debit = line.LineSubTotal, Credit = 0, Description = line.Description, VendorId = bill.VendorId });
            }
            else
            {
                var expenseAccountId = line.ExpenseAccountId ?? line.Item?.ExpenseAccountId ?? defaultExpenseId;
                lines.Add(new JournalEntryLine { AccountId = expenseAccountId, Debit = line.LineSubTotal, Credit = 0, Description = line.Description, VendorId = bill.VendorId });
            }
        }

        if (bill.TaxTotal > 0)
        {
            var taxAccountId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.PurchaseTaxReceivable, ct);
            lines.Add(new JournalEntryLine { AccountId = taxAccountId, Debit = bill.TaxTotal, Credit = 0, Description = "Purchase tax paid" });
        }

        lines.Add(new JournalEntryLine { AccountId = apAccountId, Debit = 0, Credit = bill.Total, Description = $"Bill {bill.BillNumber}", VendorId = bill.VendorId });

        var entry = await _journal.CreateSystemEntryAsync(companyId, bill.BillDate, JournalSourceType.VendorBill, bill.Id, $"Bill {bill.BillNumber}", lines, postedBy, ct);

        bill.JournalEntryId = entry.Id;
        bill.Status = BillStatus.Approved;

        await _db.SaveChangesAsync(ct);
        return bill;
    }

    public async Task VoidAsync(Guid companyId, Guid billId, string? voidedBy, CancellationToken ct = default)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId && b.CompanyId == companyId, ct)
            ?? throw new DomainException("Bill not found.");

        if (bill.AmountPaid > 0)
            throw new DomainException("Cannot void a bill that has payments applied. Unapply payments first.");

        if (bill.JournalEntryId.HasValue)
            await _journal.ReverseAsync(bill.JournalEntryId.Value, DateTime.UtcNow.Date, voidedBy, ct);

        bill.Status = BillStatus.Voided;
        await _db.SaveChangesAsync(ct);
    }
}
