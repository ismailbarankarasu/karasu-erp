using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Commands.AdjustStock;

public partial class AdjustStockCommandHandler
{
    private async Task CheckCriticalStockAlertAsync(
        Guid warehouseId,
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(s =>
                s.WarehouseId == warehouseId &&
                s.ProductVariantId == productVariantId &&
                !s.IsDeleted,
                cancellationToken);

        if (stockItem is null || stockItem.MinStock <= 0 || stockItem.Quantity > stockItem.MinStock)
            return;

        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == productVariantId, cancellationToken);

        var existingAlert = await _context.StockAlertViews
            .AnyAsync(a =>
                a.TenantId == _tenantContext.TenantId &&
                a.WarehouseId == warehouseId &&
                a.ProductVariantId == productVariantId &&
                !a.IsResolved &&
                !a.IsDeleted,
                cancellationToken);

        if (!existingAlert)
        {
            await _context.StockAlertViews.AddAsync(new StockAlertView
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                WarehouseId = warehouseId,
                ProductVariantId = productVariantId,
                Quantity = stockItem.Quantity,
                MinStock = stockItem.MinStock,
                AlertAt = DateTime.UtcNow,
                IsResolved = false,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _outboxService.EnqueueAsync(
            _tenantContext.TenantId,
            "CriticalStock",
            new
            {
                WarehouseId = warehouseId,
                ProductVariantId = productVariantId,
                ProductName = variant?.Product.Name ?? "Ürün",
                Sku = variant?.Sku,
                stockItem.Quantity,
                stockItem.MinStock
            },
            cancellationToken);
    }
}
