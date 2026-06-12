using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Invoice : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; } = InvoiceType.Standard;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime? IssuedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ICollection<InvoiceLine> Lines { get; private set; } = new List<InvoiceLine>();

    public static Invoice CreateFromOrder(
        Guid tenantId,
        Order order,
        Guid customerId,
        string invoiceNumber,
        InvoiceType type)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = order.Id,
            CustomerId = customerId,
            InvoiceNumber = invoiceNumber,
            Type = type,
            Status = InvoiceStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var line in order.Lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                ProductVariantId = line.ProductVariantId,
                Description = string.Empty,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate,
                LineTotal = line.LineTotal,
                CreatedAt = DateTime.UtcNow
            });
        }

        invoice.RecalculateTotals();
        return invoice;
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Sadece taslak faturalar kesilebilir.");

        Status = InvoiceStatus.Issued;
        IssuedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Fatura zaten iptal edilmiş.");

        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Ödenmiş fatura iptal edilemez.");

        Status = InvoiceStatus.Cancelled;
    }

    private void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.Quantity * l.UnitPrice);
        TaxTotal = Lines.Sum(l => l.Quantity * l.UnitPrice * l.TaxRate / 100);
        GrandTotal = Lines.Sum(l => l.LineTotal);
    }
}

public class InvoiceLine : TenantEntity
{
    public Guid InvoiceId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public ProductVariant? ProductVariant { get; set; }
}
