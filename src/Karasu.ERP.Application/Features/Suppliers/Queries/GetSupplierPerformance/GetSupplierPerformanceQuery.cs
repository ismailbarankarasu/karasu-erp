using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Queries.GetSupplierPerformance;

public record GetSupplierPerformanceQuery(Guid SupplierId) : IRequest<Result<SupplierPerformanceDto>>;

public record SupplierPerformanceDto(
    Guid SupplierId,
    string SupplierName,
    int TotalOrders,
    int CompletedOrders,
    int PartialOrders,
    decimal TotalSpend,
    decimal OnTimeDeliveryRate,
    decimal FillRate,
    decimal AverageDeliveryDays,
    decimal Rating);

public class GetSupplierPerformanceQueryHandler : IRequestHandler<GetSupplierPerformanceQuery, Result<SupplierPerformanceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetSupplierPerformanceQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SupplierPerformanceDto>> Handle(
        GetSupplierPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SupplierId &&
                                      s.TenantId == _tenantContext.TenantId &&
                                      !s.IsDeleted, cancellationToken);

        if (supplier is null)
            return Result<SupplierPerformanceDto>.Failure("Tedarikçi bulunamadı.", "SUPPLIER_NOT_FOUND");

        var orders = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Lines)
            .Where(p => p.SupplierId == request.SupplierId &&
                        p.TenantId == _tenantContext.TenantId &&
                        !p.IsDeleted &&
                        p.Status != PurchaseOrderStatus.Draft &&
                        p.Status != PurchaseOrderStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var completedOrders = orders.Count(p => p.Status == PurchaseOrderStatus.Received);
        var partialOrders = orders.Count(p => p.Status == PurchaseOrderStatus.PartiallyReceived);
        var totalSpend = orders.Sum(p => p.GrandTotal);

        var onTimeCount = orders.Count(p =>
            p.ReceivedAt.HasValue &&
            p.ExpectedDate.HasValue &&
            p.ReceivedAt.Value.Date <= p.ExpectedDate.Value.Date);

        var receivedWithDates = orders.Where(p => p.ReceivedAt.HasValue && p.ExpectedDate.HasValue).ToList();
        var onTimeRate = receivedWithDates.Count > 0
            ? Math.Round((decimal)onTimeCount / receivedWithDates.Count * 100, 2)
            : 0;

        var totalOrdered = orders.SelectMany(p => p.Lines.Where(l => !l.IsDeleted)).Sum(l => l.Quantity);
        var totalReceived = orders.SelectMany(p => p.Lines.Where(l => !l.IsDeleted)).Sum(l => l.ReceivedQty);
        var fillRate = totalOrdered > 0
            ? Math.Round(totalReceived / totalOrdered * 100, 2)
            : 0;

        var deliveryDays = orders
            .Where(p => p.ReceivedAt.HasValue)
            .Select(p => (p.ReceivedAt!.Value - p.CreatedAt).TotalDays)
            .ToList();

        var avgDeliveryDays = deliveryDays.Count > 0
            ? Math.Round((decimal)deliveryDays.Average(), 2)
            : 0;

        return Result<SupplierPerformanceDto>.Success(new SupplierPerformanceDto(
            supplier.Id,
            supplier.Name,
            totalOrders,
            completedOrders,
            partialOrders,
            totalSpend,
            onTimeRate,
            fillRate,
            avgDeliveryDays,
            supplier.Rating));
    }
}
