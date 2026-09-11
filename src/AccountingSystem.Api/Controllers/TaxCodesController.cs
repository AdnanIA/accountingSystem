using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Tax;
using AccountingSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record TaxCodeRequest(string Code, string Name, decimal RatePercent, TaxType Type, Guid TaxPayableOrReceivableAccountId);

public class TaxCodesController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public TaxCodesController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TaxCode>>> GetAll(CancellationToken ct)
        => Ok(await _db.TaxCodes.Where(t => t.CompanyId == CompanyId && t.IsActive).ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<TaxCode>> Create(TaxCodeRequest request, CancellationToken ct)
    {
        var taxCode = new TaxCode
        {
            CompanyId = CompanyId,
            Code = request.Code,
            Name = request.Name,
            RatePercent = request.RatePercent,
            Type = request.Type,
            TaxPayableOrReceivableAccountId = request.TaxPayableOrReceivableAccountId
        };
        _db.TaxCodes.Add(taxCode);
        await _db.SaveChangesAsync(ct);
        return Ok(taxCode);
    }
}
