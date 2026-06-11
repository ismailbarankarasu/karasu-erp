using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Identity.Entities;
using Karasu.ERP.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public RoleRepository(ApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct)
    {
        var query = _context.Roles.AsNoTracking();

        if (!_tenantContext.IsSuperAdmin)
            query = query.Where(r => r.TenantId == _tenantContext.TenantId);

        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(
                r.Id,
                r.Name!,
                r.Description,
                r.IsSystemRole,
                _context.RolePermissions.Count(rp => rp.RoleId == r.Id)))
            .ToListAsync(ct);
    }

    public async Task<RoleDetailDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct)
    {
        var role = await FindRoleAsync(roleId, ct);
        if (role is null) return null;

        var permissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Join(_context.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Module, p.Entity, p.Action, p.Description))
            .ToListAsync(ct);

        return new RoleDetailDto(role.Id, role.Name!, role.Description, role.IsSystemRole, permissions);
    }

    public async Task<(bool Success, Guid? RoleId, string? Error)> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct)
    {
        if (_tenantContext.TenantId == Guid.Empty && !_tenantContext.IsSuperAdmin)
            return (false, null, "Tenant context gerekli.");

        var normalizedName = dto.Name.ToUpperInvariant();
        var exists = await _context.Roles.AnyAsync(r =>
            r.NormalizedName == normalizedName &&
            r.TenantId == _tenantContext.TenantId, ct);

        if (exists)
            return (false, null, "Bu rol adı zaten kullanılıyor.");

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = dto.Name,
            NormalizedName = normalizedName,
            Description = dto.Description,
            IsSystemRole = false
        };

        _context.Roles.Add(role);

        if (dto.PermissionIds?.Count > 0)
            await AssignPermissionsAsync(role.Id, dto.PermissionIds, ct);

        await _context.SaveChangesAsync(ct);
        return (true, role.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateRoleAsync(Guid roleId, UpdateRoleDto dto, CancellationToken ct)
    {
        var role = await FindRoleAsync(roleId, ct);
        if (role is null) return (false, "Rol bulunamadı.");
        if (role.IsSystemRole) return (false, "Sistem rolleri düzenlenemez.");

        role.Name = dto.Name;
        role.NormalizedName = dto.Name.ToUpperInvariant();
        role.Description = dto.Description;

        await _context.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateRolePermissionsAsync(
        Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken ct)
    {
        var role = await FindRoleAsync(roleId, ct);
        if (role is null) return (false, "Rol bulunamadı.");

        var existing = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        _context.RolePermissions.RemoveRange(existing);

        await AssignPermissionsAsync(roleId, permissionIds, ct);
        await _context.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct) =>
        await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Entity).ThenBy(p => p.Action)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Module, p.Entity, p.Action, p.Description))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken ct)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.EntityId.HasValue)
            query = query.Where(a => a.EntityId == filter.EntityId.Value);

        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogDto(a.Id, a.UserId, a.EntityType, a.EntityId, a.Action, a.CreatedAt, a.IpAddress))
            .ToListAsync(ct);
    }

    private async Task<ApplicationRole?> FindRoleAsync(Guid roleId, CancellationToken ct)
    {
        var query = _context.Roles.AsQueryable();

        if (!_tenantContext.IsSuperAdmin)
            query = query.Where(r => r.TenantId == _tenantContext.TenantId);

        return await query.FirstOrDefaultAsync(r => r.Id == roleId, ct);
    }

    private async Task AssignPermissionsAsync(Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken ct)
    {
        var validIds = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var permissionId in validIds)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
    }
}
