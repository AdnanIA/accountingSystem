using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Parties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record CustomerRequest(string Code, string Name, string? Email, string? Phone, int PaymentTermsDays, decimal CreditLimit, string CurrencyCode);

public class CustomersController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public CustomersController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Customer>>> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Customers.Where(c => c.CompanyId == CompanyId);
        if (!includeInactive) query = query.Where(c => c.IsActive);
        return Ok(await query.OrderBy(c => c.Name).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Customer>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == CompanyId, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Create(CustomerRequest request, CancellationToken ct)
    {
        var customer = new Customer
        {
            CompanyId = CompanyId,
            Code = request.Code,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PaymentTermsDays = request.PaymentTermsDays,
            CreditLimit = request.CreditLimit,
            CurrencyCode = request.CurrencyCode
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CustomerRequest request, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == CompanyId, ct);
        if (customer is null) return NotFound();

        customer.Code = request.Code;
        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.PaymentTermsDays = request.PaymentTermsDays;
        customer.CreditLimit = request.CreditLimit;
        customer.CurrencyCode = request.CurrencyCode;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == CompanyId, ct);
        if (customer is null) return NotFound();
        customer.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
