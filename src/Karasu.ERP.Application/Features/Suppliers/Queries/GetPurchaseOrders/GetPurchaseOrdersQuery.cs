using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Queries.GetPurchaseOrders;

public record GetPurchaseOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? SupplierId = null,
    PurchaseOrderStatus? Status = null) : IRequest<Result<PaginatedList<PurchaseOrderListDto>>>;

public record PurchaseOrderListDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    decimal GrandTotal,
    DateTime? ExpectedDate,
    DateTime? ReceivedAt,
    DateTime CreatedAt);

public class GetPurchaseOrdersQueryHandler : IRequestHandler<GetPurchaseOrdersQuery, Result<PaginatedList<PurchaseOrderListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetPurchaseOrdersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<PurchaseOrderListDto>>> Handle(
        GetPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);

        if (request.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PurchaseOrderListDto(
                p.Id,
                p.PoNumber,
                p.SupplierId,
                p.Supplier.Name,
                p.Status,
                p.GrandTotal,
                p.ExpectedDate,
                p.ReceivedAt,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<PurchaseOrderListDto>>.Success(
            new PaginatedList<PurchaseOrderListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
