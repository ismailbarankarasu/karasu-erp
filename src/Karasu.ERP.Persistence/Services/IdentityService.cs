using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Identity.Entities;
using Karasu.ERP.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Persistence.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<(bool Success, AuthUserDto? User, string? Error)> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
            return (false, null, "Geçersiz e-posta veya şifre.");

        if (!await _userManager.CheckPasswordAsync(user, password))
            return (false, null, "Geçersiz e-posta veya şifre.");

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var dto = await BuildAuthUserDto(user, ct);
        return (true, dto, null);
    }

    public async Task<(bool Success, AuthUserDto? User, string? Error)> RegisterTenantAsync(
        string companyName, string slug, string email, string password, string fullName, CancellationToken ct)
    {
        var slugExists = await _context.Tenants.IgnoreQueryFilters()
            .AnyAsync(t => t.Slug == slug && !t.IsDeleted, ct);
        if (slugExists)
            return (false, null, "Bu slug zaten kullanılıyor.");

        if (await _userManager.FindByEmailAsync(email) is not null)
            return (false, null, "Bu e-posta zaten kayıtlı.");

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = companyName,
            Slug = slug.ToLowerInvariant(),
            BusinessType = BusinessType.Retail,
            Plan = SubscriptionPlan.Starter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var branchId = Guid.NewGuid();
        var branch = new Branch
        {
            Id = branchId,
            TenantId = tenantId,
            Name = "Merkez",
            Code = "MAIN",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var defaultWarehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            Name = "Ana Depo",
            Code = "MAIN-WH",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Identity normalizes Name → NormalizedName globally unique; tenant suffix required.
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = $"CompanyOwner_{tenantId:N}",
            IsSystemRole = true,
            Description = "Firma sahibi — tam yetki"
        };

        var defaultUnit = new Unit
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Adet",
            Symbol = "AD",
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        _context.Branches.Add(branch);
        _context.Warehouses.Add(defaultWarehouse);
        _context.Units.Add(defaultUnit);

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return (false, null, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        var roleResult = await _roleManager.CreateAsync(role);
        if (!roleResult.Succeeded)
            return (false, null, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

        await _context.Set<IdentityUserRole<Guid>>().AddAsync(new IdentityUserRole<Guid>
        {
            UserId = user.Id,
            RoleId = role.Id
        }, ct);

        await AssignDefaultPermissionsToRole(role.Id, ct);
        await _context.SaveChangesAsync(ct);

        return (true, await BuildAuthUserDto(user, ct), null);
    }

    public async Task<AuthUserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildAuthUserDto(user, ct);
    }

    public async Task<IList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Array.Empty<string>();

        var roles = await _userManager.GetRolesAsync(user);
        var roleIds = await _roleManager.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct);

        return await GetPermissionsForRoles(roleIds, ct);
    }

    private async Task<AuthUserDto> BuildAuthUserDto(ApplicationUser user, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var roleIds = await _roleManager.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct);
        var permissions = await GetPermissionsForRoles(roleIds, ct);

        return new AuthUserDto(user.Id, user.TenantId, user.Email!, user.FullName, roles, permissions);
    }

    private async Task<IList<string>> GetPermissionsForRoles(IList<Guid> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0)
            return Array.Empty<string>();

        return await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Name)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task AssignDefaultPermissionsToRole(Guid roleId, CancellationToken ct)
    {
        var permissions = await _context.Permissions.ToListAsync(ct);
        foreach (var permission in permissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }
    }
}
