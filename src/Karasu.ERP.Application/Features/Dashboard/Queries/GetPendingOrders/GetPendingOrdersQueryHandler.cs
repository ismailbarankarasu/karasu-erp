using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetPendingOrders;

public class GetPendingOrdersQueryHandler : IRequestHandler<GetPendingOrdersQuery, Result<List<PendingOrderDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetPendingOrdersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<PendingOrderDto>>> Handle(
        GetPendingOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await DashboardQueryHelper.ApplyPendingOrderFilter(
                DashboardQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), _tenantContext.TenantId))
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .Select(o => new PendingOrderDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Customer != null ? o.Customer.FullName : null,
                o.GrandTotal,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<PendingOrderDto>>.Success(orders);
    }
}
