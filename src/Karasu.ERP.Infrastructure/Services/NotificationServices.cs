using System.Text.Json;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;

namespace Karasu.ERP.Infrastructure.Services;

public class OutboxService : IOutboxService
{
    private readonly IApplicationDbContext _context;

    public OutboxService(IApplicationDbContext context) => _context = context;

    public async Task EnqueueAsync(Guid tenantId, string eventType, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        _context.OutboxMessages.Add(OutboxMessage.Create(tenantId, eventType, json));
        await _context.SaveChangesAsync(ct);
    }
}

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantNotificationPublisher _publisher;

    public NotificationService(
        IApplicationDbContext context,
        ITenantNotificationPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        NotificationCreateRequest request,
        CancellationToken ct)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = request.UserId,
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            PayloadJson = request.PayloadJson,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);
        return notification.Id;
    }

    public Task PublishRealtimeAsync(
        Guid tenantId,
        string eventName,
        object payload,
        CancellationToken ct) =>
        _publisher.PublishToTenantAsync(tenantId, eventName, payload, ct);
}
