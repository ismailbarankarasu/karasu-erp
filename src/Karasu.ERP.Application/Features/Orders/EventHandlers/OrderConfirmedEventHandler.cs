using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Events;
using MediatR;

namespace Karasu.ERP.Application.Features.Orders.EventHandlers;

/// <summary>
/// Sipariş onaylandığında: stok rezervasyonu, dashboard cache invalidation, SignalR bildirimi.
/// </summary>
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly ICacheService _cacheService;
    private readonly ITenantNotificationPublisher _notificationPublisher;

    public OrderConfirmedEventHandler(
        ICacheService cacheService,
        ITenantNotificationPublisher notificationPublisher)
    {
        _cacheService = cacheService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        // Stok düşümü ConfirmOrderCommandHandler içinde transaction ile yapılır.
        // TODO: Outbox mesajı — raporlama projection güncellemesi

        await _cacheService.RemoveByPatternAsync(
            $"{notification.TenantId}:dashboard:*",
            cancellationToken);

        foreach (var line in notification.Lines)
        {
            await _cacheService.RemoveAsync(
                $"{notification.TenantId}:stock:item:*:{line.ProductVariantId}",
                cancellationToken);
        }

        await _notificationPublisher.PublishToTenantAsync(
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
