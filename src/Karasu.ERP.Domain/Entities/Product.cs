using Karasu.ERP.Domain.Common;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Domain.Entities;

public class Product : TenantEntity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid UnitId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TaxRate { get; set; } = 20m;
    public decimal MinStock { get; set; }
    public string? ImageUrl { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit Unit { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

public class ProductVariant : TenantEntity
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string AttributesJson { get; set; } = "{}";
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string? ImageUrl { get; set; }

    public Product Product { get; set; } = null!;
}

public class Category : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }

    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}

public class Brand : TenantEntity
{
    public string Name { get; set; } = string.Empty;
}

public class Unit : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}
