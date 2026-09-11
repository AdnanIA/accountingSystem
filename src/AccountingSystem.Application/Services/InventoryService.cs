using AccountingSystem.Application.Common.Interfaces;
using AccountingSystem.Domain.Entities.Inventory;
using AccountingSystem.Domain.Enums;
using AccountingSystem.Domain.Exceptions;

namespace AccountingSystem.Application.Services;

public interface IInventoryService
{
    /// <summary>Receives stock (e.g. from a posted vendor bill) and returns the new weighted-average unit cost.</summary>
    Task<decimal> ReceiveStockAsync(Item item, DateTime date, decimal quantity, decimal unitCost, string referenceType, Guid referenceId, CancellationToken ct = default);

    /// <summary>Issues stock (e.g. from a posted sales invoice) and returns the total cost of goods sold for the issued quantity.</summary>
    Task<decimal> IssueStockAsync(Item item, DateTime date, decimal quantity, string referenceType, Guid referenceId, CancellationToken ct = default);
}

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _db;

    public InventoryService(IApplicationDbContext db) => _db = db;

    public Task<decimal> ReceiveStockAsync(Item item, DateTime date, decimal quantity, decimal unitCost, string referenceType, Guid referenceId, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new DomainException("Received quantity must be positive.");

        var newQuantity = item.QuantityOnHand + quantity;
        var newValue = (item.QuantityOnHand * item.AverageCost) + (quantity * unitCost);
        item.AverageCost = newQuantity == 0 ? 0 : Math.Round(newValue / newQuantity, 4);
        item.QuantityOnHand = newQuantity;

        _db.StockTransactions.Add(new StockTransaction
        {
            ItemId = item.Id,
            TransactionDate = date,
            Type = StockTransactionType.PurchaseReceipt,
            Quantity = quantity,
            UnitCost = unitCost,
            RunningQuantity = item.QuantityOnHand,
            RunningValue = newValue,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        });

        return Task.FromResult(item.AverageCost);
    }

    public Task<decimal> IssueStockAsync(Item item, DateTime date, decimal quantity, string referenceType, Guid referenceId, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new DomainException("Issued quantity must be positive.");
        if (item.QuantityOnHand < quantity)
            throw new DomainException($"Insufficient stock for item '{item.Name}': on hand {item.QuantityOnHand}, requested {quantity}.");

        var costOfGoodsSold = Math.Round(quantity * item.AverageCost, 2);
        item.QuantityOnHand -= quantity;
        var newValue = item.QuantityOnHand * item.AverageCost;

        _db.StockTransactions.Add(new StockTransaction
        {
            ItemId = item.Id,
            TransactionDate = date,
            Type = StockTransactionType.SaleIssue,
            Quantity = -quantity,
            UnitCost = item.AverageCost,
            RunningQuantity = item.QuantityOnHand,
            RunningValue = newValue,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        });

        return Task.FromResult(costOfGoodsSold);
    }
}
