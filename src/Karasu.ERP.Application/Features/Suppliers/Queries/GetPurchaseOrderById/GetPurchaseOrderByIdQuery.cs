using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Queries.GetPurchaseOrderById;

public record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<Result<PurchaseOrderDetailDto>>;

public record PurchaseOrderLineDto(
    Guid Id,
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal LineTotal,
    decimal ReceivedQty);

public record PurchaseOrderDetailDto(
    Guid Id,
    string PoNumber,
    Guid SupplierId,
    string SupplierName,
    PurchaseOrderStatus Status,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    DateTime? ExpectedDate,
    DateTime? ReceivedAt,
    string? Notes,
    List<PurchaseOrderLineDto> Lines);

public class GetPurchaseOrderByIdQueryHandler : IRequestHandler<GetPurchaseOrderByIdQuery, Result<PurchaseOrderDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetPurchaseOrderByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PurchaseOrderDetailDto>> Handle(
        GetPurchaseOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var po = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.TenantId == _tenantContext.TenantId && !p.IsDeleted)
            .Select(p => new PurchaseOrderDetailDto(
                p.Id,
                p.PoNumber,
                p.SupplierId,
                p.Supplier.Name,
                p.Status,
                p.SubTotal,
                p.TaxTotal,
                p.GrandTotal,
                p.ExpectedDate,
                p.ReceivedAt,
                p.Notes,
                p.Lines.Where(l => !l.IsDeleted).Select(l => new PurchaseOrderLineDto(
                    l.Id, l.ProductVariantId, l.Quantity, l.UnitPrice, l.TaxRate, l.LineTotal, l.ReceivedQty)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return po is null
            ? Result<PurchaseOrderDetailDto>.Failure("Satın alma siparişi bulunamadı.", "PO_NOT_FOUND")
            : Result<PurchaseOrderDetailDto>.Success(po);
    }
}
