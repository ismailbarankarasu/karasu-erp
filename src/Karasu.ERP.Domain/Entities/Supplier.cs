using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Supplier : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal Balance { get; set; }
    public decimal Rating { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => Array.Empty<IDomainEvent>();
    public void ClearDomainEvents() { }
}

public class PurchaseOrder : TenantEntity, IAggregateRoot
{
    public Guid SupplierId { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderLine> Lines { get; private set; } = new List<PurchaseOrderLine>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => Array.Empty<IDomainEvent>();
    public void ClearDomainEvents() { }

    public static PurchaseOrder Create(Guid tenantId, Guid supplierId, string poNumber, DateTime? expectedDate, string? notes)
    {
        return new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SupplierId = supplierId,
            PoNumber = poNumber,
            Status = PurchaseOrderStatus.Draft,
            ExpectedDate = expectedDate,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddLine(Guid productVariantId, decimal quantity, decimal unitPrice, decimal taxRate)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Sadece taslak siparişlere satır eklenebilir.");

        var lineTotal = quantity * unitPrice * (1 + taxRate / 100);
        Lines.Add(new PurchaseOrderLine
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            PurchaseOrderId = Id,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            LineTotal = lineTotal,
            ReceivedQty = 0
        });

        RecalculateTotals();
    }

    public void Send()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Sadece taslak siparişler gönderilebilir.");

        if (!Lines.Any(l => !l.IsDeleted))
            throw new InvalidOperationException("Siparişte en az bir satır olmalıdır.");

        Status = PurchaseOrderStatus.Sent;
    }

    public void Receive(IReadOnlyList<(Guid LineId, decimal Quantity)> receipts)
    {
        if (Status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Cancelled or PurchaseOrderStatus.Received)
            throw new InvalidOperationException($"Sipariş {Status} durumunda mal kabul edilemez.");

        foreach (var (lineId, quantity) in receipts)
        {
            if (quantity <= 0) continue;

            var line = Lines.FirstOrDefault(l => l.Id == lineId && !l.IsDeleted)
                ?? throw new InvalidOperationException("Geçersiz satır.");

            var remaining = line.Quantity - line.ReceivedQty;
            if (quantity > remaining)
                throw new InvalidOperationException($"Satır için kabul miktarı kalan miktarı aşamaz: {lineId}");

            line.ReceivedQty += quantity;
        }

        var allReceived = Lines.Where(l => !l.IsDeleted).All(l => l.ReceivedQty >= l.Quantity);
        Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;

        if (allReceived)
            ReceivedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException("Bu sipariş iptal edilemez.");

        Status = PurchaseOrderStatus.Cancelled;
    }

    private void RecalculateTotals()
    {
        var activeLines = Lines.Where(l => !l.IsDeleted).ToList();
        SubTotal = activeLines.Sum(l => l.Quantity * l.UnitPrice);
        TaxTotal = activeLines.Sum(l => l.Quantity * l.UnitPrice * l.TaxRate / 100);
        GrandTotal = SubTotal + TaxTotal;
    }
}

public class PurchaseOrderLine : TenantEntity
{
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }
    public decimal ReceivedQty { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}
