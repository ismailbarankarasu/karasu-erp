using Karasu.ERP.Domain.Entities;

namespace Karasu.ERP.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; set; }
    bool IsSuperAdmin { get; set; }
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Permissions { get; }
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiry, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<string> GenerateOrderNumberAsync(CancellationToken ct);
    Task AddAsync(Order order, CancellationToken ct);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct);
    Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> TaxNumberExistsAsync(string taxNumber, Guid? excludeId, CancellationToken ct);
    Task AddAsync(Customer customer, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface ITenantNotificationPublisher
{
    Task PublishToTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct);
}
