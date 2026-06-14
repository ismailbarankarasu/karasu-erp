using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetBranchComparison;

public class GetBranchComparisonQueryHandler : IRequestHandler<GetBranchComparisonQuery, Result<List<BranchComparisonDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetBranchComparisonQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<BranchComparisonDto>>> Handle(
        GetBranchComparisonQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;
        var cacheKey = $"{tenantId}:dashboard:branch-comparison:{now:yyyy-MM}";

        var comparison = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadBranchComparisonAsync(tenantId, now, cancellationToken),
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return Result<List<BranchComparisonDto>>.Success(comparison);
    }

    private async Task<List<BranchComparisonDto>> LoadBranchComparisonAsync(
        Guid tenantId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var (monthStart, monthEnd) = DashboardQueryHelper.GetMonthRange(now);

        return await DashboardQueryHelper.ApplySalesOrderFilter(
                DashboardQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId))
            .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
            .GroupBy(o => new { o.BranchId, o.Branch.Name })
            .Select(g => new BranchComparisonDto(
                g.Key.BranchId,
                g.Key.Name,
                g.Sum(x => x.GrandTotal),
                g.Count()))
            .OrderByDescending(x => x.TotalSales)
            .ToListAsync(cancellationToken);
    }
}
