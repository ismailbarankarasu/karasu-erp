using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Domain.Events;

namespace Karasu.ERP.Domain.Entities;

public class Order : TenantEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public OrderType Type { get; set; } = OrderType.Sale;
    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string? Notes { get; set; }

    public Branch Branch { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ICollection<OrderLine> Lines { get; private set; } = new List<OrderLine>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    public static Order Create(Guid tenantId, Guid branchId, Guid? customerId, string orderNumber)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            CustomerId = customerId,
            OrderNumber = orderNumber,
            Status = OrderStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddLine(Guid productVariantId, decimal quantity, decimal unitPrice, decimal taxRate, decimal discount = 0)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Sadece taslak siparişlere satır eklenebilir.");

        var lineTotal = (quantity * unitPrice - discount) * (1 + taxRate / 100);
        Lines.Add(new OrderLine
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrderId = Id,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            Discount = discount,
            LineTotal = lineTotal
        });

        RecalculateTotals();
    }

    public void Confirm(Guid userId)
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Sipariş {Status} durumunda onaylanamaz.");

        var previousStatus = Status;
        Status = OrderStatus.Confirmed;
        StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrderId = Id,
            FromStatus = previousStatus,
            ToStatus = OrderStatus.Confirmed,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
        });

        AddDomainEvent(new OrderConfirmedEvent(Id, TenantId, BranchId, Lines.Select(l => new OrderLineSnapshot(
            l.ProductVariantId, l.Quantity, l.UnitPrice)).ToList()));
    }

    public void Cancel(Guid userId, string? reason = null)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new InvalidOperationException("Bu sipariş iptal edilemez.");

        var previousStatus = Status;
        Status = OrderStatus.Cancelled;
        StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrderId = Id,
            FromStatus = previousStatus,
            ToStatus = OrderStatus.Cancelled,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            Note = reason
        });
    }

    private void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.Quantity * l.UnitPrice - l.Discount);
        TaxTotal = Lines.Sum(l => (l.Quantity * l.UnitPrice - l.Discount) * l.TaxRate / 100);
        DiscountTotal = Lines.Sum(l => l.Discount);
        GrandTotal = SubTotal + TaxTotal;
    }

    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

public class OrderLine : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

public class OrderStatusHistory : TenantEntity
{
    public Guid OrderId { get; set; }
    public OrderStatus FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public Guid ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Note { get; set; }

    public Order Order { get; set; } = null!;
}
