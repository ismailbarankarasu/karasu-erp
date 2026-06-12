using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Identity.Entities;
using Karasu.ERP.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karasu.ERP.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.IsSuperAdmin = true;

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();
        await SeedPermissionsAsync(context);
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>
        {
            // Dashboard
            Perm("Dashboard", "Summary", "View"),
            // Products
            Perm("Product", "Product", "View"), Perm("Product", "Product", "Create"),
            Perm("Product", "Product", "Update"), Perm("Product", "Product", "Delete"),
            Perm("Product", "Product", "Import"), Perm("Product", "Product", "Export"),
            // Orders
            Perm("Order", "Order", "View"), Perm("Order", "Order", "Create"),
            Perm("Order", "Order", "Update"), Perm("Order", "Order", "Delete"),
            Perm("Order", "Order", "Confirm"), Perm("Order", "Order", "Cancel"),
            // Quotes
            Perm("Quote", "Quote", "View"), Perm("Quote", "Quote", "Create"),
            Perm("Quote", "Quote", "Update"), Perm("Quote", "Quote", "Convert"),
            // Invoices
            Perm("Invoice", "Invoice", "View"), Perm("Invoice", "Invoice", "Create"),
            // Customers
            Perm("Customer", "Customer", "View"), Perm("Customer", "Customer", "Create"),
            Perm("Customer", "Customer", "Update"), Perm("Customer", "Customer", "Delete"),
            // POS
            Perm("Pos", "Session", "Open"), Perm("Pos", "Session", "Close"),
            Perm("Pos", "Session", "View"),
            Perm("Pos", "Sale", "Sell"), Perm("Pos", "Sale", "Return"),
            // Stock
            Perm("Stock", "Item", "View"), Perm("Stock", "Item", "Adjust"),
            Perm("Stock", "Transfer", "Create"), Perm("Stock", "Count", "Create"),
            // Warehouse
            Perm("Warehouse", "Warehouse", "View"), Perm("Warehouse", "Warehouse", "Create"),
            Perm("Warehouse", "Warehouse", "Update"), Perm("Warehouse", "Warehouse", "Delete"),
            // Finance
            Perm("Finance", "Account", "View"), Perm("Finance", "Account", "Create"),
            // Reports
            Perm("Report", "Sales", "View"), Perm("Report", "Sales", "Export"),
            // Audit
            Perm("Audit", "Log", "View"),
            // Branch
            Perm("Branch", "Branch", "View"), Perm("Branch", "Branch", "Create"),
            Perm("Branch", "Branch", "Update"), Perm("Branch", "Branch", "Delete"),
            // User management
            Perm("User", "User", "View"), Perm("User", "User", "Create"),
            Perm("User", "User", "Update"), Perm("User", "User", "Delete"),
            Perm("Role", "Role", "View"), Perm("Role", "Role", "Create"), Perm("Role", "Role", "Update"),
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }

    private static Permission Perm(string module, string entity, string action) => new()
    {
        Id = Guid.NewGuid(),
        Module = module,
        Entity = entity,
        Action = action,
        Description = $"{module} {entity} {action}"
    };
}
