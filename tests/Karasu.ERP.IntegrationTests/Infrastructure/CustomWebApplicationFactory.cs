using Karasu.ERP.Identity.Entities;
using Karasu.ERP.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karasu.ERP.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InMemoryDatabaseName"] = _databaseName
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        SeedTestPermissions(db);

        return host;
    }

    private static void SeedTestPermissions(ApplicationDbContext db)
    {
        if (db.Permissions.Any()) return;

        db.Permissions.AddRange(
            new Permission { Id = Guid.NewGuid(), Module = "Role", Entity = "Role", Action = "View", Description = "View roles" },
            new Permission { Id = Guid.NewGuid(), Module = "Role", Entity = "Role", Action = "Create", Description = "Create roles" },
            new Permission { Id = Guid.NewGuid(), Module = "Role", Entity = "Role", Action = "Update", Description = "Update roles" },
            new Permission { Id = Guid.NewGuid(), Module = "Audit", Entity = "Log", Action = "View", Description = "View audit logs" },
            new Permission { Id = Guid.NewGuid(), Module = "Product", Entity = "Product", Action = "View", Description = "View products" },
            new Permission { Id = Guid.NewGuid(), Module = "Product", Entity = "Product", Action = "Create", Description = "Create products" },
            new Permission { Id = Guid.NewGuid(), Module = "Product", Entity = "Product", Action = "Update", Description = "Update products" },
            new Permission { Id = Guid.NewGuid(), Module = "Product", Entity = "Product", Action = "Delete", Description = "Delete products" },
            new Permission { Id = Guid.NewGuid(), Module = "Customer", Entity = "Customer", Action = "View", Description = "View customers" },
            new Permission { Id = Guid.NewGuid(), Module = "Customer", Entity = "Customer", Action = "Create", Description = "Create customers" },
            new Permission { Id = Guid.NewGuid(), Module = "Customer", Entity = "Customer", Action = "Update", Description = "Update customers" },
            new Permission { Id = Guid.NewGuid(), Module = "Customer", Entity = "Customer", Action = "Delete", Description = "Delete customers" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "View", Description = "View orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Create", Description = "Create orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Update", Description = "Update orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Delete", Description = "Delete orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Confirm", Description = "Confirm orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Cancel", Description = "Cancel orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Quote", Entity = "Quote", Action = "View", Description = "View quotes" },
            new Permission { Id = Guid.NewGuid(), Module = "Quote", Entity = "Quote", Action = "Create", Description = "Create quotes" },
            new Permission { Id = Guid.NewGuid(), Module = "Quote", Entity = "Quote", Action = "Update", Description = "Update quotes" },
            new Permission { Id = Guid.NewGuid(), Module = "Quote", Entity = "Quote", Action = "Convert", Description = "Convert quotes" },
            new Permission { Id = Guid.NewGuid(), Module = "Invoice", Entity = "Invoice", Action = "View", Description = "View invoices" },
            new Permission { Id = Guid.NewGuid(), Module = "Invoice", Entity = "Invoice", Action = "Create", Description = "Create invoices" },
            new Permission { Id = Guid.NewGuid(), Module = "Stock", Entity = "Item", Action = "View", Description = "View stock" },
            new Permission { Id = Guid.NewGuid(), Module = "Stock", Entity = "Item", Action = "Adjust", Description = "Adjust stock" },
            new Permission { Id = Guid.NewGuid(), Module = "Warehouse", Entity = "Warehouse", Action = "View", Description = "View warehouses" },
            new Permission { Id = Guid.NewGuid(), Module = "Finance", Entity = "Account", Action = "View", Description = "View finance" },
            new Permission { Id = Guid.NewGuid(), Module = "Finance", Entity = "Account", Action = "Create", Description = "Create finance" },
            new Permission { Id = Guid.NewGuid(), Module = "Dashboard", Entity = "Summary", Action = "View", Description = "View dashboard" },
            new Permission { Id = Guid.NewGuid(), Module = "Report", Entity = "Sales", Action = "View", Description = "View reports" },
            new Permission { Id = Guid.NewGuid(), Module = "Report", Entity = "Sales", Action = "Export", Description = "Export reports" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Employee", Action = "View", Description = "View HR" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Employee", Action = "Create", Description = "Create HR" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Employee", Action = "Update", Description = "Update HR" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Leave", Action = "Create", Description = "Create leave" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Leave", Action = "Approve", Description = "Approve leave" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Shift", Action = "Create", Description = "Create shift" },
            new Permission { Id = Guid.NewGuid(), Module = "Hr", Entity = "Payroll", Action = "Create", Description = "Create payroll" },
            new Permission { Id = Guid.NewGuid(), Module = "Supplier", Entity = "Supplier", Action = "View", Description = "View suppliers" },
            new Permission { Id = Guid.NewGuid(), Module = "Supplier", Entity = "Supplier", Action = "Create", Description = "Create suppliers" },
            new Permission { Id = Guid.NewGuid(), Module = "Supplier", Entity = "Supplier", Action = "Update", Description = "Update suppliers" },
            new Permission { Id = Guid.NewGuid(), Module = "PurchaseOrder", Entity = "PurchaseOrder", Action = "View", Description = "View PO" },
            new Permission { Id = Guid.NewGuid(), Module = "PurchaseOrder", Entity = "PurchaseOrder", Action = "Create", Description = "Create PO" },
            new Permission { Id = Guid.NewGuid(), Module = "PurchaseOrder", Entity = "PurchaseOrder", Action = "Receive", Description = "Receive PO" });
        db.SaveChanges();
    }
}
