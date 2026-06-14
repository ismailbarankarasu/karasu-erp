using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerOrders;

public record GetCustomerOrdersQuery(Guid CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PaginatedList<CustomerOrderDto>>>;

public record CustomerOrderDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal GrandTotal,
    DateTime CreatedAt);

public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQuery, Result<PaginatedList<CustomerOrderDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCustomerOrdersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<CustomerOrderDto>>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var customerExists = await _context.Customers.AnyAsync(
            c => c.Id == request.CustomerId && c.TenantId == _tenantContext.TenantId && !c.IsDeleted,
            cancellationToken);

        if (!customerExists)
            return Result<PaginatedList<CustomerOrderDto>>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == request.CustomerId &&
                        o.TenantId == _tenantContext.TenantId &&
                        !o.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new CustomerOrderDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.GrandTotal,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<CustomerOrderDto>>.Success(
            new PaginatedList<CustomerOrderDto>(items, totalCount, request.Page, request.PageSize));
    }
}
