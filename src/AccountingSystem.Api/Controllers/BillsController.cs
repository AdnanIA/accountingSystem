using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.DTOs;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Purchases;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public class BillsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IBillService _billService;

    public BillsController(IApplicationDbContext db, IBillService billService)
    {
        _db = db;
        _billService = billService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BillDto>>> GetAll([FromQuery] Guid? vendorId, CancellationToken ct)
    {
        var query = _db.Bills.Where(b => b.CompanyId == CompanyId);
        if (vendorId.HasValue) query = query.Where(b => b.VendorId == vendorId.Value);

        var bills = await query.Include(b => b.Vendor).Include(b => b.Lines)
            .OrderByDescending(b => b.BillDate).ThenByDescending(b => b.BillNumber)
            .Take(200).ToListAsync(ct);

        return Ok(bills.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillDto>> GetById(Guid id, CancellationToken ct)
    {
        var bill = await _db.Bills.Include(b => b.Vendor).Include(b => b.Lines)
            .FirstOrDefaultAsync(b => b.Id == id && b.CompanyId == CompanyId, ct);
        return bill is null ? NotFound() : Ok(ToDto(bill));
    }

    [HttpPost]
    public async Task<ActionResult<BillDto>> Create(CreateBillRequest request, CancellationToken ct)
    {
        var bill = await _billService.CreateDraftAsync(CompanyId, request, ct);
        var full = await _db.Bills.Include(b => b.Vendor).Include(b => b.Lines).FirstAsync(b => b.Id == bill.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = bill.Id }, ToDto(full));
    }

    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<BillDto>> Post(Guid id, CancellationToken ct)
    {
        await _billService.PostAsync(CompanyId, id, CurrentUserName, ct);
        var full = await _db.Bills.Include(b => b.Vendor).Include(b => b.Lines).FirstAsync(b => b.Id == id, ct);
        return Ok(ToDto(full));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct)
    {
        await _billService.VoidAsync(CompanyId, id, CurrentUserName, ct);
        return NoContent();
    }

    private static BillDto ToDto(Bill b) => new(
        b.Id, b.BillNumber, b.VendorId, b.Vendor.Name, b.BillDate, b.DueDate, b.Status,
        b.SubTotal, b.TaxTotal, b.Total, b.AmountPaid, b.Balance,
        b.Lines.OrderBy(l => l.LineNumber).Select(l => new BillLineDto(l.Id, l.Description, l.Quantity, l.UnitCost, l.TaxAmount, l.LineTotal)).ToList());
}
