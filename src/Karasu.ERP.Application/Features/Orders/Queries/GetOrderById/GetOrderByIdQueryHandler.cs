using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetOrderByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OrderDetailDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == request.Id && o.TenantId == _tenantContext.TenantId && !o.IsDeleted)
            .Select(o => new OrderDetailDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.Type,
                o.BranchId,
                o.Branch.Name,
                o.CustomerId,
                o.Customer != null ? o.Customer.FullName : null,
                o.SubTotal,
                o.TaxTotal,
                o.DiscountTotal,
                o.GrandTotal,
                o.Notes,
                o.CreatedAt,
                o.UpdatedAt,
                o.Lines.Select(l => new OrderLineDetailDto(
                    l.Id,
                    l.ProductVariantId,
                    l.ProductVariant.Product.Name,
                    l.ProductVariant.Sku,
                    l.Quantity,
                    l.UnitPrice,
                    l.TaxRate,
                    l.Discount,
                    l.LineTotal)).ToList(),
                o.StatusHistory
                    .OrderBy(h => h.ChangedAt)
                    .Select(h => new OrderStatusHistoryDto(
                        h.FromStatus,
                        h.ToStatus,
                        h.ChangedBy,
                        h.ChangedAt,
                        h.Note))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
            return Result<OrderDetailDto>.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        return Result<OrderDetailDto>.Success(order);
    }
}
