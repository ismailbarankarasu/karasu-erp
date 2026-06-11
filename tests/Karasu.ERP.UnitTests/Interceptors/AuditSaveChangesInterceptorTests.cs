using FluentAssertions;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Infrastructure.Services;
using Karasu.ERP.Persistence.Context;
using Karasu.ERP.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karasu.ERP.UnitTests.Interceptors;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task SaveChanges_should_create_audit_log_for_new_product()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext { TenantId = tenantId };
        var currentUser = new TestCurrentUser(Guid.NewGuid());

        var interceptor = new AuditSaveChangesInterceptor(
            tenantContext,
            currentUser,
            new HttpContextAccessor(),
            new DateTimeService());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"AuditTest_{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new ApplicationDbContext(options, tenantContext, currentUser);

        context.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Sku = "SKU-AUDIT",
            Name = "Audit Product",
            UnitId = Guid.NewGuid(),
            SalePrice = 100,
            PurchasePrice = 50,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var logs = await context.AuditLogs.IgnoreQueryFilters().ToListAsync();
        logs.Should().ContainSingle(a => a.EntityType == nameof(Product) && a.Action == "Create");
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public TestCurrentUser(Guid userId) => UserId = userId;
        public Guid? UserId { get; }
        public string? Email => "audit@test.com";
        public IReadOnlyList<string> Permissions => Array.Empty<string>();
    }
}
