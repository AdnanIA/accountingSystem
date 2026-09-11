using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Domain.Entities.Accounting;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Entities.Sales;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Application.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateDraftAsync(Guid companyId, CreateInvoiceRequest request, CancellationToken ct = default);
    Task<Invoice> PostAsync(Guid companyId, Guid invoiceId, string? postedBy, CancellationToken ct = default);
    Task VoidAsync(Guid companyId, Guid invoiceId, string? voidedBy, CancellationToken ct = default);
}

public class InvoiceService : IInvoiceService
{
    private readonly IApplicationDbContext _db;
    private readonly IJournalEntryService _journal;
    private readonly INumberSequenceService _numbers;
    private readonly ISystemAccountResolver _systemAccounts;
    private readonly IInventoryService _inventory;

    public InvoiceService(IApplicationDbContext db, IJournalEntryService journal, INumberSequenceService numbers, ISystemAccountResolver systemAccounts, IInventoryService inventory)
    {
        _db = db;
        _journal = journal;
        _numbers = numbers;
        _systemAccounts = systemAccounts;
        _inventory = inventory;
    }

    public async Task<Invoice> CreateDraftAsync(Guid companyId, CreateInvoiceRequest request, CancellationToken ct = default)
    {
        if (request.Lines is null || request.Lines.Count == 0)
            throw new DomainException("An invoice requires at least one line.");

        var taxCodeIds = request.Lines.Where(l => l.TaxCodeId.HasValue).Select(l => l.TaxCodeId!.Value).Distinct().ToList();
        var taxCodes = await _db.TaxCodes.Where(t => taxCodeIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);

        var invoiceNumber = await _numbers.GetNextNumberAsync(companyId, "INV", "INV", ct);

        var invoice = new Invoice
        {
            CompanyId = companyId,
            InvoiceNumber = invoiceNumber,
            CustomerId = request.CustomerId,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            Status = InvoiceStatus.Draft,
            CurrencyCode = request.CurrencyCode,
            ExchangeRateToBase = request.ExchangeRateToBase,
            Memo = request.Memo,
            Terms = request.Terms
        };

        int lineNo = 1;
        foreach (var l in request.Lines)
        {
            var line = new InvoiceLine
            {
                LineNumber = lineNo++,
                ItemId = l.ItemId,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent,
                TaxCodeId = l.TaxCodeId,
                RevenueAccountId = l.RevenueAccountId
            };

            var subTotal = Math.Round(l.Quantity * l.UnitPrice * (1 - l.DiscountPercent / 100m), 2);
            if (l.TaxCodeId.HasValue && taxCodes.TryGetValue(l.TaxCodeId.Value, out var taxCode))
                line.TaxAmount = Math.Round(subTotal * taxCode.RatePercent / 100m, 2);

            invoice.Lines.Add(line);
        }

        invoice.SubTotal = invoice.Lines.Sum(l => l.LineSubTotal);
        invoice.TaxTotal = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.Total = invoice.SubTotal + invoice.TaxTotal;

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<Invoice> PostAsync(Guid companyId, Guid invoiceId, string? postedBy, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        if (invoice.Status != InvoiceStatus.Draft)
            throw new DomainException("Only draft invoices can be posted.");

        var arAccountId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.AccountsReceivable, ct);
        var defaultRevenueId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.DefaultSalesRevenue, ct);

        var lines = new List<JournalEntryLine>
        {
            new() { AccountId = arAccountId, Debit = invoice.Total, Credit = 0, Description = $"Invoice {invoice.InvoiceNumber}", CustomerId = invoice.CustomerId }
        };

        decimal totalCogs = 0;
        Guid? cogsAccountId = null;

        foreach (var line in invoice.Lines)
        {
            var revenueAccountId = line.RevenueAccountId ?? line.Item?.IncomeAccountId ?? defaultRevenueId;
            lines.Add(new JournalEntryLine
            {
                AccountId = revenueAccountId,
                Debit = 0,
                Credit = line.LineSubTotal,
                Description = line.Description,
                CustomerId = invoice.CustomerId
            });

            if (line.Item is { Type: ItemType.Inventory } item)
            {
                var cogs = await _inventory.IssueStockAsync(item, invoice.InvoiceDate, line.Quantity, nameof(Invoice), invoice.Id, ct);
                totalCogs += cogs;
                cogsAccountId ??= item.ExpenseAccountId ?? await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.DefaultCogs, ct);
            }
        }

        if (invoice.TaxTotal > 0)
        {
            var taxAccountId = await _systemAccounts.ResolveAsync(companyId, SystemAccountKeys.SalesTaxPayable, ct);
            lines.Add(new JournalEntryLine { AccountId = taxAccountId, Debit = 0, Credit = invoice.TaxTotal, Description = "Sales tax collected" });
        }

        if (totalCogs > 0 && cogsAccountId.HasValue)
        {
            var inventoryAssetId = invoice.Lines.Where(l => l.Item?.InventoryAssetAccountId != null)
                .Select(l => l.Item!.InventoryAssetAccountId!.Value).FirstOrDefault();
            lines.Add(new JournalEntryLine { AccountId = cogsAccountId.Value, Debit = totalCogs, Credit = 0, Description = "Cost of goods sold" });
            lines.Add(new JournalEntryLine { AccountId = inventoryAssetId, Debit = 0, Credit = totalCogs, Description = "Inventory reduction" });
        }

        var entry = await _journal.CreateSystemEntryAsync(companyId, invoice.InvoiceDate, JournalSourceType.SalesInvoice, invoice.Id, $"Invoice {invoice.InvoiceNumber}", lines, postedBy, ct);

        invoice.JournalEntryId = entry.Id;
        invoice.Status = InvoiceStatus.Sent;

        await _db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task VoidAsync(Guid companyId, Guid invoiceId, string? voidedBy, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == companyId, ct)
            ?? throw new DomainException("Invoice not found.");

        if (invoice.AmountPaid > 0)
            throw new DomainException("Cannot void an invoice that has payments applied. Unapply payments first.");

        if (invoice.JournalEntryId.HasValue)
            await _journal.ReverseAsync(invoice.JournalEntryId.Value, DateTime.UtcNow.Date, voidedBy, ct);

        invoice.Status = InvoiceStatus.Voided;
        await _db.SaveChangesAsync(ct);
    }
}
