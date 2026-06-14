using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetDashboardCriticalStock;

public class GetDashboardCriticalStockQueryHandler
    : IRequestHandler<GetDashboardCriticalStockQuery, Result<List<DashboardCriticalStockDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetDashboardCriticalStockQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<DashboardCriticalStockDto>>> Handle(
        GetDashboardCriticalStockQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{_tenantContext.TenantId}:dashboard:critical-stock";

        var items = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadCriticalStockAsync(cancellationToken),
            TimeSpan.FromMinutes(2),
            cancellationToken);

        return Result<List<DashboardCriticalStockDto>>.Success(items);
    }

    private async Task<List<DashboardCriticalStockDto>> LoadCriticalStockAsync(
        CancellationToken cancellationToken)
    {
        return await _context.StockItems
            .AsNoTracking()
            .Where(s =>
                s.TenantId == _tenantContext.TenantId &&
                !s.IsDeleted &&
                s.MinStock > 0 &&
                s.Quantity <= s.MinStock)
            .OrderBy(s => s.Quantity - s.MinStock)
            .Take(10)
            .Select(s => new DashboardCriticalStockDto(
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
