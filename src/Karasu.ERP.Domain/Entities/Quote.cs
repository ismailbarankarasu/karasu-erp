using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Quote : TenantEntity
{
    public Guid? BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public decimal SubTotal { get; private set; }
    public decimal TaxTotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal GrandTotal { get; private set; }
    public DateTime? ValidUntil { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedOrderId { get; set; }

    public Branch? Branch { get; set; }
    public Customer? Customer { get; set; }
    public Order? ConvertedOrder { get; set; }
    public ICollection<QuoteLine> Lines { get; private set; } = new List<QuoteLine>();

    public static Quote Create(Guid tenantId, Guid? branchId, Guid? customerId, string quoteNumber, DateTime? validUntil)
    {
        return new Quote
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            CustomerId = customerId,
            QuoteNumber = quoteNumber,
            ValidUntil = validUntil,
            Status = QuoteStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddLine(Guid productVariantId, decimal quantity, decimal unitPrice, decimal taxRate, decimal discount = 0)
    {
        if (Status is QuoteStatus.Converted or QuoteStatus.Cancelled)
            throw new InvalidOperationException("Bu teklif güncellenemez.");

        var lineTotal = (quantity * unitPrice - discount) * (1 + taxRate / 100);
        Lines.Add(new QuoteLine
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            QuoteId = Id,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxRate = taxRate,
            Discount = discount,
            LineTotal = lineTotal
        });

        RecalculateTotals();
    }

    public void UpdateDraft(
        Guid? branchId,
        Guid? customerId,
        string? notes,
        DateTime? validUntil,
        IEnumerable<(Guid ProductVariantId, decimal Quantity, decimal UnitPrice, decimal TaxRate, decimal Discount)> lines)
    {
        if (Status != QuoteStatus.Draft)
            throw new InvalidOperationException("Sadece taslak teklifler güncellenebilir.");

        BranchId = branchId;
        CustomerId = customerId;
        Notes = notes;
        ValidUntil = validUntil;
        Lines.Clear();
        RecalculateTotals();

        foreach (var line in lines)
            AddLine(line.ProductVariantId, line.Quantity, line.UnitPrice, line.TaxRate, line.Discount);
    }

    public void MarkConverted(Guid orderId)
    {
        if (Status == QuoteStatus.Converted)
            throw new InvalidOperationException("Teklif zaten siparişe dönüştürülmüş.");

        if (Status is QuoteStatus.Cancelled or QuoteStatus.Rejected)
            throw new InvalidOperationException("Bu teklif siparişe dönüştürülemez.");

        if (ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow)
            throw new InvalidOperationException("Teklif süresi dolmuş.");

        Status = QuoteStatus.Converted;
        ConvertedOrderId = orderId;
    }

    private void RecalculateTotals()
    {
        SubTotal = Lines.Sum(l => l.Quantity * l.UnitPrice - l.Discount);
        TaxTotal = Lines.Sum(l => (l.Quantity * l.UnitPrice - l.Discount) * l.TaxRate / 100);
        DiscountTotal = Lines.Sum(l => l.Discount);
        GrandTotal = SubTotal + TaxTotal;
    }
}

public class QuoteLine : TenantEntity
{
    public Guid QuoteId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }

    public Quote Quote { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}
