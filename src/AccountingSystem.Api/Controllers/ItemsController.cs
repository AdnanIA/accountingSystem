using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Api.Controllers;

public record ItemRequest(
    string SKU, string Name, string? Description, ItemType Type, decimal SalesPrice, decimal PurchaseCost,
    Guid? IncomeAccountId, Guid? ExpenseAccountId, Guid? InventoryAssetAccountId, Guid? DefaultTaxCodeId, decimal ReorderPoint);

public class ItemsController : ApiControllerBase
{
    private readonly IApplicationDbContext _db;

    public ItemsController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Item>>> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Items.Where(i => i.CompanyId == CompanyId);
        if (!includeInactive) query = query.Where(i => i.IsActive);
        return Ok(await query.OrderBy(i => i.Name).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Item>> GetById(Guid id, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Item>> Create(ItemRequest request, CancellationToken ct)
    {
        var item = new Item
        {
            CompanyId = CompanyId,
            SKU = request.SKU,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            SalesPrice = request.SalesPrice,
            PurchaseCost = request.PurchaseCost,
            IncomeAccountId = request.IncomeAccountId,
            ExpenseAccountId = request.ExpenseAccountId,
            InventoryAssetAccountId = request.InventoryAssetAccountId,
            DefaultTaxCodeId = request.DefaultTaxCodeId,
            ReorderPoint = request.ReorderPoint
        };
        _db.Items.Add(item);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpGet("{id:guid}/stock-transactions")]
    public async Task<ActionResult<List<StockTransaction>>> GetStockTransactions(Guid id, CancellationToken ct)
        => Ok(await _db.StockTransactions.Where(s => s.ItemId == id).OrderByDescending(s => s.TransactionDate).ToListAsync(ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == CompanyId, ct);
        if (item is null) return NotFound();
        item.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
