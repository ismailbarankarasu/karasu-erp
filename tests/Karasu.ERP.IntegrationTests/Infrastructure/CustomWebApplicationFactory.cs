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
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Confirm", Description = "Confirm orders" },
            new Permission { Id = Guid.NewGuid(), Module = "Order", Entity = "Order", Action = "Cancel", Description = "Cancel orders" });
        db.SaveChanges();
    }
}
