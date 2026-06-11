using Karasu.ERP.Shared.Models;

namespace Karasu.ERP.Application.Common.Interfaces;

public record StockOrderLine(Guid ProductVariantId, decimal Quantity);

public interface IStockService
{
    Task<Guid?> GetDefaultWarehouseIdForBranchAsync(Guid branchId, CancellationToken ct);

    Task<Guid?> GetDefaultWarehouseIdAsync(CancellationToken ct);

    Task<Result> EnsureStockItemAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal minStock,
        CancellationToken ct);

    Task<Result> DeductForOrderAsync(
        Guid warehouseId,
        Guid orderId,
        IReadOnlyList<StockOrderLine> lines,
        CancellationToken ct);

    Task<Result> RestoreForOrderAsync(
        Guid warehouseId,
        Guid orderId,
        IReadOnlyList<StockOrderLine> lines,
        CancellationToken ct);

    Task<Result> AdjustStockAsync(
        Guid warehouseId,
        Guid productVariantId,
        decimal quantityDelta,
        string? note,
        CancellationToken ct);
}
