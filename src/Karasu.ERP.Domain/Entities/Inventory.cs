using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Warehouse : TenantEntity
{
    public Guid BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public Branch Branch { get; set; } = null!;
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}

public class StockItem : TenantEntity
{
    public Guid WarehouseId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal MinStock { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();

    public decimal AvailableQuantity => Quantity - ReservedQuantity;

    public void Deduct(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Miktar sıfırdan büyük olmalıdır.");

        if (AvailableQuantity < amount)
            throw new InvalidOperationException("Yetersiz stok.");

        Quantity -= amount;
    }

    public void Restore(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Miktar sıfırdan büyük olmalıdır.");

        Quantity += amount;
    }

    public void Adjust(decimal delta)
    {
        if (delta == 0)
            throw new ArgumentOutOfRangeException(nameof(delta), "Miktar sıfır olamaz.");

        if (delta < 0 && Quantity + delta < 0)
            throw new InvalidOperationException("Stok miktarı negatif olamaz.");

        Quantity += delta;
    }
}

public class StockMovement : TenantEntity
{
    public Guid StockItemId { get; set; }
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }

    public StockItem StockItem { get; set; } = null!;
}

public class StockTransfer : TenantEntity
{
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Pending;
    public Guid RequestedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }

    public Warehouse FromWarehouse { get; set; } = null!;
    public Warehouse ToWarehouse { get; set; } = null!;
    public ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();
}

public class StockTransferLine : TenantEntity
{
    public Guid TransferId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal Quantity { get; set; }

    public StockTransfer Transfer { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

public class StockCount : TenantEntity
{
    public Guid WarehouseId { get; set; }
    public StockCountStatus Status { get; set; } = StockCountStatus.InProgress;
    public Guid CountedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }

    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}

public class StockCountLine : TenantEntity
{
    public Guid CountId { get; set; }
    public Guid ProductVariantId { get; set; }
    public decimal SystemQty { get; set; }
    public decimal? CountedQty { get; set; }

    public StockCount Count { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
}

public static class StockReferenceTypes
{
    public const string Order = "Order";
    public const string Adjustment = "Adjustment";
    public const string Transfer = "Transfer";
    public const string Count = "Count";
}
