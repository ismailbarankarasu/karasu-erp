using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetRecentActivities;

public class GetRecentActivitiesQueryHandler : IRequestHandler<GetRecentActivitiesQuery, Result<List<RecentActivityDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetRecentActivitiesQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<RecentActivityDto>>> Handle(
        GetRecentActivitiesQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var cacheKey = $"{tenantId}:dashboard:recent-activities";

        var activities = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadRecentActivitiesAsync(tenantId, cancellationToken),
            TimeSpan.FromMinutes(1),
            cancellationToken);

        return Result<List<RecentActivityDto>>.Success(activities);
    }

    private async Task<List<RecentActivityDto>> LoadRecentActivitiesAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .Select(a => new RecentActivityDto(
                a.EntityType,
                a.Action,
                a.EntityId,
                a.UserId,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
