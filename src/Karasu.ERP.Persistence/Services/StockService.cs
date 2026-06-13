using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Persistence.Services;

public class StockService : IStockService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public StockService(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Guid?> GetDefaultWarehouseIdForBranchAsync(Guid branchId, CancellationToken ct)
    {
        var warehouseId = await FindDefaultWarehouseIdForBranchAsync(branchId, ct);
        if (warehouseId != Guid.Empty)
            return warehouseId;

        var branchExists = await _context.Branches.AnyAsync(
            b => b.Id == branchId && b.TenantId == _tenantContext.TenantId && !b.IsDeleted,
            ct);

        if (!branchExists)
            return null;

        return await EnsureDefaultWarehouseForBranchAsync(branchId, ct);
    }

    public async Task<Guid?> GetDefaultWarehouseIdAsync(CancellationToken ct)
    {
        var warehouseId = await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.TenantId == _tenantContext.TenantId && w.IsDefault && !w.IsDeleted)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

        if (warehouseId != Guid.Empty)
            return warehouseId;

        var branchId = await _context.Branches
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted && b.IsActive)
            .OrderByDescending(b => b.Code == "MAIN")
            .Select(b => b.Id)
            .FirstOrDefaultAsync(ct);

        if (branchId == Guid.Empty)
            return null;

        return await EnsureDefaultWarehouseForBranchAsync(branchId, ct);
    }

    private async Task<Guid> FindDefaultWarehouseIdForBranchAsync(Guid branchId, CancellationToken ct) =>
        await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.BranchId == branchId && w.IsDefault && !w.IsDeleted)
            .Select(w => w.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<Guid> EnsureDefaultWarehouseForBranchAsync(Guid branchId, CancellationToken ct)
    {
        var existingId = await FindDefaultWarehouseIdForBranchAsync(branchId, ct);
        if (existingId != Guid.Empty)
            return existingId;

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            BranchId = branchId,
            Name = "Ana Depo",
            Code = "MAIN-WH",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Warehouses.AddAsync(warehouse, ct);
        return warehouse.Id;
    }

    public async Task<Result> EnsureStockItemAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal minStock,
        CancellationToken ct)
    {
        var exists = await _context.StockItems
            .AnyAsync(s =>
                s.WarehouseId == warehouseId &&
                s.ProductVariantId == productVariantId &&
                !s.IsDeleted,
                ct);

        if (exists)
            return Result.Success();

        var stockItem = new StockItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            WarehouseId = warehouseId,
            ProductVariantId = productVariantId,
            Quantity = 0,
            ReservedQuantity = 0,
            MinStock = minStock,
            CreatedAt = DateTime.UtcNow
        };

        await _context.StockItems.AddAsync(stockItem, ct);
        return Result.Success();
    }

    public async Task<Result> ReserveForOrderAsync(
        Guid warehouseId,
        Guid orderId,
        IReadOnlyList<StockOrderLine> lines,
        CancellationToken ct)
    {
        if (lines.Count == 0)
            return Result.Success();

        var variantIds = lines.Select(l => l.ProductVariantId).Distinct().ToList();

        var stockItems = await _context.StockItems
            .Where(s => s.WarehouseId == warehouseId && variantIds.Contains(s.ProductVariantId))
            .ToDictionaryAsync(s => s.ProductVariantId, ct);

        foreach (var line in lines)
        {
            if (!stockItems.TryGetValue(line.ProductVariantId, out var stockItem))
                return Result.Failure("Stok kaydı bulunamadı.", "STOCK_ITEM_NOT_FOUND");

            try
            {
                stockItem.Reserve(line.Quantity);
            }
            catch (InvalidOperationException)
            {
                return Result.Failure("Yetersiz stok.", "INSUFFICIENT_STOCK");
            }
        }

        return Result.Success();
    }

    public async Task<Result> DeductForOrderAsync(
        Guid warehouseId,
        Guid orderId,
        IReadOnlyList<StockOrderLine> lines,
        CancellationToken ct)
    {
        if (lines.Count == 0)
            return Result.Success();

        var variantIds = lines.Select(l => l.ProductVariantId).Distinct().ToList();

        var stockItems = await _context.StockItems
            .Where(s => s.WarehouseId == warehouseId && variantIds.Contains(s.ProductVariantId))
            .ToDictionaryAsync(s => s.ProductVariantId, ct);

        foreach (var line in lines)
        {
            if (!stockItems.TryGetValue(line.ProductVariantId, out var stockItem))
                return Result.Failure("Stok kaydı bulunamadı.", "STOCK_ITEM_NOT_FOUND");

            try
            {
                if (stockItem.ReservedQuantity >= line.Quantity)
                    stockItem.FulfillReservation(line.Quantity);
                else
                    stockItem.Deduct(line.Quantity);
            }
            catch (InvalidOperationException)
            {
                return Result.Failure("Yetersiz stok.", "INSUFFICIENT_STOCK");
            }

            await _context.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                StockItemId = stockItem.Id,
                Type = StockMovementType.Out,
                Quantity = line.Quantity,
                ReferenceType = StockReferenceTypes.Order,
                ReferenceId = orderId,
                Note = "Sipariş onayı",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        return Result.Success();
    }

    public async Task<Result> ReleaseForOrderAsync(
        Guid warehouseId,
        Guid orderId,
        IReadOnlyList<StockOrderLine> lines,
        CancellationToken ct)
    {
        if (lines.Count == 0)
            return Result.Success();

        var variantIds = lines.Select(l => l.ProductVariantId).Distinct().ToList();

        var stockItems = await _context.StockItems
            .Where(s => s.WarehouseId == warehouseId && variantIds.Contains(s.ProductVariantId))
            .ToDictionaryAsync(s => s.ProductVariantId, ct);

        foreach (var line in lines)
        {
            if (!stockItems.TryGetValue(line.ProductVariantId, out var stockItem))
                return Result.Failure("Stok kaydı bulunamadı.", "STOCK_ITEM_NOT_FOUND");

            var releaseAmount = Math.Min(line.Quantity, stockItem.ReservedQuantity);
            if (releaseAmount <= 0)
                continue;

            try
            {
                stockItem.ReleaseReservation(releaseAmount);
            }
            catch (InvalidOperationException)
            {
                return Result.Failure("Yetersiz rezervasyon.", "INSUFFICIENT_RESERVATION");
            }
        }

        return Result.Success();
    }

    public async Task<Result> RestoreForOrderAsync(
        Guid warehouseId,
        Guid orderId,
        IReadOnlyList<StockOrderLine> lines,
        CancellationToken ct)
    {
        if (lines.Count == 0)
            return Result.Success();

        var variantIds = lines.Select(l => l.ProductVariantId).Distinct().ToList();

        var stockItems = await _context.StockItems
            .Where(s => s.WarehouseId == warehouseId && variantIds.Contains(s.ProductVariantId))
            .ToDictionaryAsync(s => s.ProductVariantId, ct);

        foreach (var line in lines)
        {
            if (!stockItems.TryGetValue(line.ProductVariantId, out var stockItem))
                return Result.Failure("Stok kaydı bulunamadı.", "STOCK_ITEM_NOT_FOUND");

            stockItem.Restore(line.Quantity);

            await _context.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                StockItemId = stockItem.Id,
                Type = StockMovementType.Return,
                Quantity = line.Quantity,
                ReferenceType = StockReferenceTypes.Order,
                ReferenceId = orderId,
                Note = "Sipariş iptali",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        return Result.Success();
    }

    public async Task<Result> AdjustStockAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantityDelta,
        string? note,
        CancellationToken ct,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(s =>
                s.WarehouseId == warehouseId &&
                s.ProductVariantId == productVariantId &&
                !s.IsDeleted,
                ct);

        if (stockItem is null)
            return Result.Failure("Stok kaydı bulunamadı.", "STOCK_ITEM_NOT_FOUND");

        try
        {
            stockItem.Adjust(quantityDelta);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_STOCK_ADJUSTMENT");
        }

        var movementType = quantityDelta > 0 ? StockMovementType.In : StockMovementType.Adjustment;

        await _context.StockMovements.AddAsync(new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            StockItemId = stockItem.Id,
            Type = movementType,
            Quantity = Math.Abs(quantityDelta),
            ReferenceType = referenceType ?? StockReferenceTypes.Adjustment,
            ReferenceId = referenceId,
            Note = note,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return Result.Success();
    }

    public async Task<Result> CompleteTransferAsync(Guid transferId, CancellationToken ct)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(
                t => t.Id == transferId &&
                     t.TenantId == _tenantContext.TenantId &&
                     !t.IsDeleted,
                ct);

        if (transfer is null)
            return Result.Failure("Transfer bulunamadı.", "TRANSFER_NOT_FOUND");

        if (transfer.Status != StockTransferStatus.Pending)
            return Result.Failure("Sadece bekleyen transferler tamamlanabilir.", "TRANSFER_INVALID_STATUS");

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
            return Result.Failure("Kaynak ve hedef depo aynı olamaz.", "TRANSFER_SAME_WAREHOUSE");

        if (transfer.Lines.Count == 0)
            return Result.Failure("Transfer satırı bulunamadı.", "TRANSFER_EMPTY");

        var variantIds = transfer.Lines.Select(l => l.ProductVariantId).Distinct().ToList();

        var sourceItems = await _context.StockItems
            .Where(s => s.WarehouseId == transfer.FromWarehouseId && variantIds.Contains(s.ProductVariantId))
            .ToDictionaryAsync(s => s.ProductVariantId, ct);

        foreach (var line in transfer.Lines)
        {
            if (!sourceItems.TryGetValue(line.ProductVariantId, out var sourceItem))
                return Result.Failure("Kaynak depoda stok kaydı bulunamadı.", "STOCK_ITEM_NOT_FOUND");

            try
            {
                sourceItem.Deduct(line.Quantity);
            }
            catch (InvalidOperationException)
            {
                return Result.Failure("Kaynak depoda yetersiz stok.", "INSUFFICIENT_STOCK");
            }

            await _context.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                StockItemId = sourceItem.Id,
                Type = StockMovementType.Transfer,
                Quantity = line.Quantity,
                ReferenceType = StockReferenceTypes.Transfer,
                ReferenceId = transfer.Id,
                Note = "Depolar arası transfer (çıkış)",
                CreatedAt = DateTime.UtcNow
            }, ct);

            var destItem = await _context.StockItems
                .FirstOrDefaultAsync(s =>
                    s.WarehouseId == transfer.ToWarehouseId &&
                    s.ProductVariantId == line.ProductVariantId &&
                    !s.IsDeleted,
                    ct);

            if (destItem is null)
            {
                destItem = new StockItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId,
                    WarehouseId = transfer.ToWarehouseId,
                    ProductVariantId = line.ProductVariantId,
                    Quantity = 0,
                    ReservedQuantity = 0,
                    MinStock = sourceItem.MinStock,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.StockItems.AddAsync(destItem, ct);
            }

            destItem.Restore(line.Quantity);

            await _context.StockMovements.AddAsync(new StockMovement
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                StockItemId = destItem.Id,
                Type = StockMovementType.Transfer,
                Quantity = line.Quantity,
                ReferenceType = StockReferenceTypes.Transfer,
                ReferenceId = transfer.Id,
                Note = "Depolar arası transfer (giriş)",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        transfer.Status = StockTransferStatus.Completed;
        transfer.CompletedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public async Task<Result> CompleteCountAsync(Guid countId, CancellationToken ct)
    {
        var count = await _context.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(
                c => c.Id == countId &&
                     c.TenantId == _tenantContext.TenantId &&
                     !c.IsDeleted,
                ct);

        if (count is null)
            return Result.Failure("Sayım bulunamadı.", "COUNT_NOT_FOUND");

        if (count.Status != StockCountStatus.InProgress)
            return Result.Failure("Sadece devam eden sayımlar tamamlanabilir.", "COUNT_INVALID_STATUS");

        if (count.Lines.Count == 0)
            return Result.Failure("Sayım satırı bulunamadı.", "COUNT_EMPTY");

        if (count.Lines.Any(l => !l.CountedQty.HasValue))
            return Result.Failure("Tüm sayım satırları için sayılan miktar girilmelidir.", "COUNT_LINES_INCOMPLETE");

        foreach (var line in count.Lines)
        {
            var difference = line.CountedQty!.Value - line.SystemQty;
            if (difference == 0)
                continue;

            var adjustResult = await AdjustStockAsync(
                count.WarehouseId,
                line.ProductVariantId,
                difference,
                "Stok sayımı düzeltmesi",
                ct,
                StockReferenceTypes.Count,
                count.Id);

            if (!adjustResult.IsSuccess)
                return adjustResult;
        }

        count.Status = StockCountStatus.Completed;
        count.CompletedAt = DateTime.UtcNow;

        return Result.Success();
    }
}
