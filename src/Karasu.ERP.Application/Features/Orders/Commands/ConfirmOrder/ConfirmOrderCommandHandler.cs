using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Commands.ConfirmOrder;

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cacheService;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;
    private readonly IMediator _mediator;

    public ConfirmOrderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ICacheService cacheService,
        ITenantContext tenantContext,
        IStockService stockService,
        IMediator mediator)
    {
        _context = context;
        _currentUser = currentUser;
        _cacheService = cacheService;
        _tenantContext = tenantContext;
        _stockService = stockService;
        _mediator = mediator;
    }

    public async Task<Result> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var orderSnapshot = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(
                o => o.Id == request.OrderId && o.TenantId == _tenantContext.TenantId && !o.IsDeleted,
                cancellationToken);

        if (orderSnapshot is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        try
        {
            orderSnapshot.Confirm(_currentUser.UserId ?? Guid.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "ORDER_INVALID_STATUS");
        }

        var warehouseId = await _stockService.GetDefaultWarehouseIdForBranchAsync(
            orderSnapshot.BranchId, cancellationToken);
        if (warehouseId is null)
            return Result.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        var stockLines = orderSnapshot.Lines
            .Select(l => new StockOrderLine(l.ProductVariantId, l.Quantity))
            .ToList();

        var reserveResult = await _stockService.ReserveForOrderAsync(
            warehouseId.Value, orderSnapshot.Id, stockLines, cancellationToken);
        if (!reserveResult.IsSuccess)
            return reserveResult;

        var stockResult = await _stockService.DeductForOrderAsync(
            warehouseId.Value, orderSnapshot.Id, stockLines, cancellationToken);
        if (!stockResult.IsSuccess)
            return stockResult;

        var trackedOrder = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderSnapshot.Id, cancellationToken);

        if (trackedOrder is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        trackedOrder.Status = OrderStatus.Confirmed;
        _context.OrderStatusHistories.Add(orderSnapshot.StatusHistory.Last());
        await _context.SaveChangesAsync(cancellationToken);

        var events = orderSnapshot.DomainEvents.ToList();
        orderSnapshot.ClearDomainEvents();
        foreach (var domainEvent in events)
            await _mediator.Publish(domainEvent, cancellationToken);

        await _cacheService.RemoveByPatternAsync("dashboard:*", cancellationToken);

        return Result.Success();
    }
}
