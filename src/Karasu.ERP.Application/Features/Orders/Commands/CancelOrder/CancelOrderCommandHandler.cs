using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;
    private readonly IStockService _stockService;

    public CancelOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        ICacheService cacheService,
        IStockService stockService)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
        _stockService = stockService;
    }

    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var orderSnapshot = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(
                o => o.Id == request.OrderId && o.TenantId == _tenantContext.TenantId && !o.IsDeleted,
                cancellationToken);

        if (orderSnapshot is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        var wasConfirmed = orderSnapshot.Status == OrderStatus.Confirmed;

        try
        {
            orderSnapshot.Cancel(_currentUser.UserId ?? Guid.Empty, request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "ORDER_INVALID_STATUS");
        }

        if (wasConfirmed)
        {
            var warehouseId = await _stockService.GetDefaultWarehouseIdForBranchAsync(
                orderSnapshot.BranchId, cancellationToken);
            if (warehouseId is null)
                return Result.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

            var stockLines = orderSnapshot.Lines
                .Select(l => new StockOrderLine(l.ProductVariantId, l.Quantity))
                .ToList();

            var stockResult = await _stockService.RestoreForOrderAsync(
                warehouseId.Value, orderSnapshot.Id, stockLines, cancellationToken);
            if (!stockResult.IsSuccess)
                return stockResult;
        }

        var trackedOrder = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderSnapshot.Id, cancellationToken);

        if (trackedOrder is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        trackedOrder.Status = OrderStatus.Cancelled;
        _context.OrderStatusHistories.Add(orderSnapshot.StatusHistory.Last());
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveByPatternAsync("dashboard:*", cancellationToken);

        return Result.Success();
    }
}
