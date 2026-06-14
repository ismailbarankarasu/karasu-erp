using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetTopProducts;

public class GetTopProductsQueryHandler : IRequestHandler<GetTopProductsQuery, Result<List<TopProductDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetTopProductsQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<TopProductDto>>> Handle(
        GetTopProductsQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var periodDays = request.PeriodDays ?? 30;
        var cacheKey = $"{tenantId}:dashboard:top-products:{periodDays}";

        var products = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadTopProductsAsync(tenantId, periodDays, cancellationToken),
            TimeSpan.FromMinutes(10),
            cancellationToken);

        return Result<List<TopProductDto>>.Success(products);
    }

    private async Task<List<TopProductDto>> LoadTopProductsAsync(
        Guid tenantId,
        int periodDays,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rangeStart = now.Date.AddDays(-periodDays + 1);

        return await _context.OrderLines
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId &&
                        !l.IsDeleted &&
                        DashboardQueryHelper.SalesOrderStatuses.Contains(l.Order.Status) &&
                        !l.Order.IsDeleted &&
                        l.Order.CreatedAt >= rangeStart &&
                        l.Order.CreatedAt < now.Date.AddDays(1))
            .GroupBy(l => new
            {
                l.ProductVariant.Product.Name,
                l.ProductVariant.Sku
            })
            .Select(g => new TopProductDto(
                g.Key.Name,
                g.Key.Sku,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.QuantitySold)
            .Take(10)
            .ToListAsync(cancellationToken);
    }
}
