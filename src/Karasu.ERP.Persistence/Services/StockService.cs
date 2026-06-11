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
        CancellationToken ct)
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
            ReferenceType = StockReferenceTypes.Adjustment,
            Note = note,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return Result.Success();
    }
}
