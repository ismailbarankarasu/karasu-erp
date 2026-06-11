using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Karasu.ERP.Persistence.Context;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Karasu.ERP.Api"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection") ??
            "Server=localhost,1433;Database=KarasuERP;User Id=sa;Password=Karasu@2026!;TrustServerCertificate=True");

        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantContext(), new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; }
        public bool IsSuperAdmin { get; set; } = true;
    }

    private sealed class DesignTimeCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public string? Email => null;
        public IReadOnlyList<string> Permissions => Array.Empty<string>();
    }
}
