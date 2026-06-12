using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockAlerts;

public class GetStockAlertsQueryHandler : IRequestHandler<GetStockAlertsQuery, Result<List<StockAlertDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetStockAlertsQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<StockAlertDto>>> Handle(
        GetStockAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{_tenantContext.TenantId}:stock:alerts:critical";

        var allAlerts = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadAlertsAsync(null, cancellationToken),
            TimeSpan.FromMinutes(2),
            cancellationToken);

        var alerts = request.WarehouseId.HasValue
            ? allAlerts.Where(a => a.WarehouseId == request.WarehouseId.Value).ToList()
            : allAlerts;

        return Result<List<StockAlertDto>>.Success(alerts);
    }

    private async Task<List<StockAlertDto>> LoadAlertsAsync(Guid? warehouseId, CancellationToken cancellationToken)
    {
        var query = _context.StockItems
            .AsNoTracking()
            .Where(s =>
                s.TenantId == _tenantContext.TenantId &&
                !s.IsDeleted &&
                s.MinStock > 0 &&
                s.Quantity <= s.MinStock);

        if (warehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == warehouseId.Value);

        return await query
            .OrderBy(s => s.Quantity - s.MinStock)
            .Select(s => new StockAlertDto(
                s.Id,
                s.WarehouseId,
                s.Warehouse.Name,
                s.ProductVariantId,
                s.ProductVariant.Product.Name,
                s.ProductVariant.Sku,
                s.Quantity,
                s.MinStock,
                s.MinStock - s.Quantity))
            .ToListAsync(cancellationToken);
    }
}
