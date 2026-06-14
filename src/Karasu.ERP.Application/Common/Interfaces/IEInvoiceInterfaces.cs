namespace Karasu.ERP.Application.Common.Interfaces;

public record EInvoiceSubmitRequest(
    Guid TenantId,
    Guid? InvoiceId,
    Guid? OrderId,
    string DocumentNumber,
    decimal GrandTotal,
    string CustomerName,
    string? CustomerTaxNumber);

public record EInvoiceSubmitResult(
    bool Success,
    string? GibUuid,
    string? ResponseJson,
    string? ErrorMessage);

public interface IEInvoiceProvider
{
    Domain.Enums.EInvoiceProvider ProviderType { get; }

    Task<EInvoiceSubmitResult> SubmitEInvoiceAsync(EInvoiceSubmitRequest request, CancellationToken ct);

    Task<EInvoiceSubmitResult> SubmitEArchiveAsync(EInvoiceSubmitRequest request, CancellationToken ct);

    Task<EInvoiceSubmitResult> SubmitEDispatchAsync(EInvoiceSubmitRequest request, CancellationToken ct);

    Task<EInvoiceSubmitResult> CheckStatusAsync(string gibUuid, CancellationToken ct);
}

public interface IEInvoiceProviderResolver
{
    IEInvoiceProvider Resolve(Domain.Enums.EInvoiceProvider provider);
}

public interface IOutboxService
{
    Task EnqueueAsync(Guid tenantId, string eventType, object payload, CancellationToken ct);
}

public interface INotificationService
{
    Task<Guid> CreateAsync(
        Guid tenantId,
        NotificationCreateRequest request,
        CancellationToken ct);

    Task PublishRealtimeAsync(
        Guid tenantId,
        string eventName,
        object payload,
        CancellationToken ct);
}

public record NotificationCreateRequest(
    Guid? UserId,
    Domain.Enums.NotificationType Type,
    string Title,
    string Message,
    string? PayloadJson = null);
