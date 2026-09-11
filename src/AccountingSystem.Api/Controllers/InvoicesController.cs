using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class InvoicesController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IApplicationDbContext db, IInvoiceService invoiceService)
    {
        _db = db;
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll([FromQuery] Guid? customerId, CancellationToken ct)
    {
        var query = _db.Invoices.Where(i => i.CompanyId == CompanyId);
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);

        var invoices = await query.Include(i => i.Customer).Include(i => i.Lines)
            .OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.InvoiceNumber)
            .Take(200).ToListAsync(ct);

        return Ok(invoices.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken ct)
    {
        var invoice = await _db.Invoices.Include(i => i.Customer).Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId, ct);
        return invoice is null ? NotFound() : Ok(ToDto(invoice));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var invoice = await _invoiceService.CreateDraftAsync(CompanyId, request, ct);
        var full = await _db.Invoices.Include(i => i.Customer).Include(i => i.Lines).FirstAsync(i => i.Id == invoice.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, ToDto(full));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<InvoiceDto>> Post(Guid id, CancellationToken ct)
    {
        await _invoiceService.PostAsync(CompanyId, id, CurrentUserName, ct);
        var full = await _db.Invoices.Include(i => i.Customer).Include(i => i.Lines).FirstAsync(i => i.Id == id, ct);
        return Ok(ToDto(full));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        await _invoiceService.VoidAsync(CompanyId, id, CurrentUserName, ct);
        return NoContent();
    }

    private static InvoiceDto ToDto(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.CustomerId, i.Customer.Name, i.InvoiceDate, i.DueDate, i.Status,
        i.SubTotal, i.TaxTotal, i.Total, i.AmountPaid, i.Balance,
        i.Lines.OrderBy(l => l.LineNumber).Select(l => new InvoiceLineDto(l.Id, l.Description, l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxAmount, l.LineTotal)).ToList());
}
