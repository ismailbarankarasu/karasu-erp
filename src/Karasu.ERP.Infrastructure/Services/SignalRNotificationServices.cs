using System.Text.Json;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Karasu.ERP.Infrastructure.Services;

public class TenantNotificationPublisher : ITenantNotificationPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public TenantNotificationPublisher(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public async Task PublishToTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var hubContext = scope.ServiceProvider.GetService<IHubContext<NotificationHub>>();
        if (hubContext is not null)
        {
            await hubContext.Clients.Group($"tenant-{tenantId}")
                .SendAsync(eventName, payload, ct);
        }
    }
}

public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}");

        await base.OnConnectedAsync();
    }
}

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox işleme hatası");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var messages = context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending && !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToList();

        foreach (var message in messages)
        {
            message.Status = OutboxMessageStatus.Processing;
            await context.SaveChangesAsync(ct);

            try
            {
                await HandleMessageAsync(message, notificationService, ct);
                message.MarkProcessed();
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogWarning(ex, "Outbox mesajı işlenemedi: {EventType}", message.EventType);
            }

            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task HandleMessageAsync(
        Domain.Entities.OutboxMessage message,
        INotificationService notificationService,
        CancellationToken ct)
    {
        switch (message.EventType)
        {
            case "OrderConfirmed":
                await notificationService.CreateAsync(
                    message.TenantId,
                    new NotificationCreateRequest(
                        null,
                        NotificationType.OrderConfirmed,
                        "Sipariş Onaylandı",
                        "Yeni bir sipariş onaylandı.",
                        message.Payload),
                    ct);
                await notificationService.PublishRealtimeAsync(
                    message.TenantId,
                    "NewOrder",
                    JsonSerializer.Deserialize<object>(message.Payload) ?? new { },
                    ct);
                break;

            case "CriticalStock":
                await notificationService.CreateAsync(
                    message.TenantId,
                    new NotificationCreateRequest(
                        null,
                        NotificationType.CriticalStock,
                        "Kritik Stok",
                        "Stok seviyesi kritik eşiğin altına düştü.",
                        message.Payload),
                    ct);
                await notificationService.PublishRealtimeAsync(
                    message.TenantId,
                    "CriticalStock",
                    JsonSerializer.Deserialize<object>(message.Payload) ?? new { },
                    ct);
                break;

            default:
                await notificationService.PublishRealtimeAsync(
                    message.TenantId,
                    message.EventType,
                    JsonSerializer.Deserialize<object>(message.Payload) ?? new { },
                    ct);
                break;
        }
    }
}
