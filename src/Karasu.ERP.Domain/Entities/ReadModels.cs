using Karasu.ERP.Domain.Common;

namespace Karasu.ERP.Domain.Entities;

public class DailySalesSummary : TenantEntity
{
    public DateTime Date { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
}

public class ProductSalesRanking : TenantEntity
{
    public Guid ProductVariantId { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;
}

public class BranchPerformanceSnapshot : TenantEntity
{
    public Guid BranchId { get; set; }
    public string Period { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }

    public Branch Branch { get; set; } = null!;
}

public class StockAlertView : TenantEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal MinStock { get; set; }
    public DateTime AlertAt { get; set; }
    public bool IsResolved { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}
