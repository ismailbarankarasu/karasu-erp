using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Events;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.EventHandlers;

/// <summary>
/// Sipariş onaylandığında: dashboard cache invalidation, outbox mesajı.
/// </summary>
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly IOutboxService _outboxService;

    public OrderConfirmedEventHandler(
        ICacheService cacheService,
        IOutboxService outboxService)
    {
        _cacheService = cacheService;
        _outboxService = outboxService;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        await _cacheService.RemoveByPatternAsync(
            $"{notification.TenantId}:dashboard:*",
            cancellationToken);

        foreach (var line in notification.Lines)
        {
            await _cacheService.RemoveAsync(
                $"{notification.TenantId}:stock:item:*:{line.ProductVariantId}",
                cancellationToken);
        }

        await _outboxService.EnqueueAsync(
            notification.TenantId,
            "OrderConfirmed",
            new
            {
                notification.OrderId,
                notification.BranchId,
                LineCount = notification.Lines.Count
            },
            cancellationToken);
    }
}
