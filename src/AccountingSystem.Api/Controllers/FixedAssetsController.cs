using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Application.Services;
using AccountingSystem.Domain.Entities.Assets;
using AccountingSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record FixedAssetRequest(
    string Code, string Name, DateTime AcquisitionDate, decimal AcquisitionCost, decimal SalvageValue, int UsefulLifeMonths,
    DepreciationMethod Method, Guid AssetAccountId, Guid AccumulatedDepreciationAccountId, Guid DepreciationExpenseAccountId);

public record DisposeAssetRequest(DateTime DisposalDate, decimal Proceeds);

public class FixedAssetsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;
    private readonly IFixedAssetService _fixedAssetService;

    public FixedAssetsController(IApplicationDbContext db, IFixedAssetService fixedAssetService)
    {
        _db = db;
        _fixedAssetService = fixedAssetService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FixedAsset>>> GetAll(CancellationToken ct)
        => Ok(await _db.FixedAssets.Where(a => a.CompanyId == CompanyId).ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<FixedAsset>> Create(FixedAssetRequest request, CancellationToken ct)
    {
        var asset = new FixedAsset
        {
            CompanyId = CompanyId,
            Code = request.Code,
            Name = request.Name,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionCost = request.AcquisitionCost,
            SalvageValue = request.SalvageValue,
            UsefulLifeMonths = request.UsefulLifeMonths,
            Method = request.Method,
            AssetAccountId = request.AssetAccountId,
            AccumulatedDepreciationAccountId = request.AccumulatedDepreciationAccountId,
            DepreciationExpenseAccountId = request.DepreciationExpenseAccountId
        };
        _db.FixedAssets.Add(asset);
        await _db.SaveChangesAsync(ct);
        return Ok(asset);
    }

    [HttpPost("run-depreciation")]
    public async Task<IActionResult> RunDepreciation([FromQuery] DateTime periodEndDate, CancellationToken ct)
    {
        var count = await _fixedAssetService.RunMonthlyDepreciationAsync(CompanyId, periodEndDate, CurrentUserName, ct);
        return Ok(new { assetsDepreciated = count });
    }

    [HttpPost("{id:guid}/dispose")]
    public async Task<IActionResult> Dispose(Guid id, DisposeAssetRequest request, CancellationToken ct)
    {
        await _fixedAssetService.DisposeAssetAsync(CompanyId, id, request.DisposalDate, request.Proceeds, CurrentUserName, ct);
        return NoContent();
    }
}
