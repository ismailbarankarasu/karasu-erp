using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Notification : TenantEntity
{
    public Guid? UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}

public class OutboxMessage : TenantEntity
{
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }

    public static OutboxMessage Create(Guid tenantId, string eventType, string payload)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            Payload = payload,
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount++;
        ErrorMessage = error;
        Status = RetryCount >= 5 ? OutboxMessageStatus.Failed : OutboxMessageStatus.Pending;
    }
}

public class InboxMessage : BaseEntity
{
    public string MessageId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
