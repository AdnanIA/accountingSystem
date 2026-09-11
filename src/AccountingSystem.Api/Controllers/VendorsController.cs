using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Parties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record VendorRequest(string Code, string Name, string? Email, string? Phone, int PaymentTermsDays, string CurrencyCode, bool Is1099Vendor);

public class VendorsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public VendorsController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Vendor>>> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Vendors.Where(v => v.CompanyId == CompanyId);
        if (!includeInactive) query = query.Where(v => v.IsActive);
        return Ok(await query.OrderBy(v => v.Name).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Vendor>> GetById(Guid id, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == CompanyId, ct);
        return vendor is null ? NotFound() : Ok(vendor);
    }

    [HttpPost]
    public async Task<ActionResult<Vendor>> Create(VendorRequest request, CancellationToken ct)
    {
        var vendor = new Vendor
        {
            CompanyId = CompanyId,
            Code = request.Code,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PaymentTermsDays = request.PaymentTermsDays,
            CurrencyCode = request.CurrencyCode,
            Is1099Vendor = request.Is1099Vendor
        };
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = vendor.Id }, vendor);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, VendorRequest request, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == CompanyId, ct);
        if (vendor is null) return NotFound();

        vendor.Code = request.Code;
        vendor.Name = request.Name;
        vendor.Email = request.Email;
        vendor.Phone = request.Phone;
        vendor.PaymentTermsDays = request.PaymentTermsDays;
        vendor.CurrencyCode = request.CurrencyCode;
        vendor.Is1099Vendor = request.Is1099Vendor;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == CompanyId, ct);
        if (vendor is null) return NotFound();
        vendor.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
