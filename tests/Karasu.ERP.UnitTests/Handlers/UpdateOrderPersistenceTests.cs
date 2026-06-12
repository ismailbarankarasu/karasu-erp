using FluentAssertions;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Infrastructure.Services;
using Karasu.ERP.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karasu.ERP.UnitTests.Handlers;

public class UpdateOrderPersistenceTests
{
    [Fact]
    public async Task Deleting_order_line_immediately_after_insert_should_work()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext { TenantId = tenantId };
        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"OrderDelete_{Guid.NewGuid():N}")
            .Options;

        await using var context = new ApplicationDbContext(options, tenantContext, currentUser);

        var order = Order.Create(tenantId, Guid.NewGuid(), null, "ORD-DEL");
        order.AddLine(Guid.NewGuid(), 1, 100, 20);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var line = await context.OrderLines.FirstAsync();
        context.OrderLines.Remove(line);
        await context.SaveChangesAsync();

        (await context.OrderLines.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Updating_order_in_new_db_context_should_work()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new TenantContext { TenantId = tenantId };
        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var databaseName = $"OrderOnlyUpdate_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        Guid orderId;
        await using (var createContext = new ApplicationDbContext(options, tenantContext, currentUser))
        {
            var newOrder = Order.Create(tenantId, Guid.NewGuid(), null, "ORD-ONLY");
            newOrder.AddLine(Guid.NewGuid(), 1, 100, 20);
            createContext.Orders.Add(newOrder);
            await createContext.SaveChangesAsync();
            orderId = newOrder.Id;
        }

        await using var updateContext = new ApplicationDbContext(options, tenantContext, currentUser);
        var order = await updateContext.Orders.FirstAsync(o => o.Id == orderId);
        order.Notes = "updated";
        await updateContext.SaveChangesAsync();

        order.Notes.Should().Be("updated");
    }

    [Fact]
    public async Task Replacing_order_lines_in_new_db_context_should_persist_in_memory_database()
    {
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var tenantContext = new TenantContext { TenantId = tenantId };
        var currentUser = new TestCurrentUser(Guid.NewGuid());
        var databaseName = $"OrderUpdate_{Guid.NewGuid():N}";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        Guid orderId;
        await using (var createContext = new ApplicationDbContext(options, tenantContext, currentUser))
        {
            var newOrder = Order.Create(tenantId, branchId, null, "ORD-TEST");
            newOrder.AddLine(variantId, 1, 100, 20);
            createContext.Orders.Add(newOrder);
            await createContext.SaveChangesAsync();
            orderId = newOrder.Id;
        }

        await using var updateContext = new ApplicationDbContext(options, tenantContext, currentUser);

        var order = await updateContext.Orders
            .Include(o => o.Lines)
            .FirstAsync(o => o.Id == orderId);

        order.UpdateDraft(
            null,
            "updated",
            new[] { (variantId, 3m, 100m, 20m, 0m) });
        await updateContext.SaveChangesAsync();

        var reloaded = await updateContext.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstAsync(o => o.Id == orderId);

        reloaded.Lines.Should().HaveCount(1);
        reloaded.Lines.Single().Quantity.Should().Be(3);
        reloaded.GrandTotal.Should().Be(360);
        reloaded.Notes.Should().Be("updated");
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public TestCurrentUser(Guid userId) => UserId = userId;
        public Guid? UserId { get; }
        public string? Email => "test@test.com";
        public IReadOnlyList<string> Permissions => Array.Empty<string>();
    }
}
