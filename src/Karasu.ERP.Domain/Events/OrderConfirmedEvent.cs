using Karasu.ERP.Domain.Common;

namespace Karasu.ERP.Domain.Events;

public record OrderLineSnapshot(Guid ProductVariantId, decimal Quantity, decimal UnitPrice);

public record OrderConfirmedEvent(
    Guid OrderId,
    Guid TenantId,
    Guid BranchId,
    IReadOnlyList<OrderLineSnapshot> Lines) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
