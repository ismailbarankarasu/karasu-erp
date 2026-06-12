using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Orders.Queries.GetOrderById;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetOrderHistory;

public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, Result<List<OrderStatusHistoryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetOrderHistoryQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<OrderStatusHistoryDto>>> Handle(
        GetOrderHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var orderExists = await _context.Orders.AnyAsync(
            o => o.Id == request.OrderId && o.TenantId == _tenantContext.TenantId && !o.IsDeleted,
            cancellationToken);

        if (!orderExists)
            return Result<List<OrderStatusHistoryDto>>.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        var history = await _context.OrderStatusHistories
            .AsNoTracking()
            .Where(h => h.OrderId == request.OrderId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OrderStatusHistoryDto(
                h.FromStatus,
                h.ToStatus,
                h.ChangedBy,
                h.ChangedAt,
                h.Note))
            .ToListAsync(cancellationToken);

        return Result<List<OrderStatusHistoryDto>>.Success(history);
    }
}
