using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, Result<PaginatedList<OrderListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetOrdersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<OrderListDto>>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.TenantId == _tenantContext.TenantId && !o.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        if (request.CustomerId.HasValue)
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        if (request.FromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(o => o.CreatedAt <= request.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(o => o.OrderNumber.Contains(request.SearchTerm));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderListDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Customer != null ? o.Customer.FullName : null,
                o.GrandTotal,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<OrderListDto>>.Success(
            new PaginatedList<OrderListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
