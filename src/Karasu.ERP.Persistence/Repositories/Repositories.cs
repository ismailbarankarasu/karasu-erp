using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Identity.Entities;
using Karasu.ERP.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTimeService _dateTime;

    public RefreshTokenRepository(ApplicationDbContext context, IDateTimeService dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task StoreAsync(Guid userId, string token, DateTime expiresAt, string? ip, CancellationToken ct)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = _dateTime.UtcNow,
            CreatedByIp = ip
        });
        await _context.SaveChangesAsync(ct);
    }

    public async Task<RefreshTokenInfo?> GetByTokenAsync(string token, CancellationToken ct)
    {
        var entity = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Token == token, ct);

        if (entity is null) return null;

        return new RefreshTokenInfo(
            entity.Id,
            entity.UserId,
            entity.Token,
            entity.ExpiresAt,
            entity.IsActive);
    }

    public async Task RevokeAsync(string token, string? replacedBy, string? ip, CancellationToken ct)
    {
        var entity = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, ct);
        if (entity is null) return;

        entity.RevokedAt = _dateTime.UtcNow;
        entity.RevokedByIp = ip;
        entity.ReplacedByToken = replacedBy;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        var tokens = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in tokens)
            token.RevokedAt = _dateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context) => _context = context;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<string> GenerateOrderNumberAsync(CancellationToken ct)
    {
        var count = await _context.Orders.CountAsync(ct);
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D5}";
    }

    public async Task AddAsync(Order order, CancellationToken ct) =>
        await _context.Orders.AddAsync(order, ct);
}

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken ct) =>
        _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode, ct);

    public Task<bool> SkuExistsAsync(string sku, Guid? excludeId, CancellationToken ct) =>
        _context.Products
            .IgnoreQueryFilters()
            .AnyAsync(p => p.Sku == sku && (!excludeId.HasValue || p.Id != excludeId.Value), ct);

    public async Task AddAsync(Product product, CancellationToken ct) =>
        await _context.Products.AddAsync(product, ct);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context) => _context = context;

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> TaxNumberExistsAsync(string taxNumber, Guid? excludeId, CancellationToken ct) =>
        _context.Customers
            .IgnoreQueryFilters()
            .AnyAsync(c => c.TaxNumber == taxNumber && (!excludeId.HasValue || c.Id != excludeId.Value), ct);

    public async Task AddAsync(Customer customer, CancellationToken ct) =>
        await _context.Customers.AddAsync(customer, ct);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
