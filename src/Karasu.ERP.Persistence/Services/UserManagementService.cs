using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Identity.Entities;
using Karasu.ERP.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Persistence.Services;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<PaginatedUsersResult> GetUsersAsync(
        Guid tenantId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _userManager.Users.Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.FullName.Contains(term) || (u.Email != null && u.Email.Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = new List<UserListItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItemDto(
                user.Id,
                user.Email!,
                user.FullName,
                user.IsActive,
                roles.ToList(),
                user.CreatedAt,
                user.LastLoginAt));
        }

        return new PaginatedUsersResult(items, totalCount, page, pageSize);
    }

    public async Task<UserDetailDto?> GetUserAsync(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct);

        if (user is null)
            return null;

        var roleNames = await _userManager.GetRolesAsync(user);
        var roleIds = await _roleManager.Roles
            .Where(r => r.TenantId == tenantId && roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(ct);

        return new UserDetailDto(
            user.Id,
            user.Email!,
            user.FullName,
            user.IsActive,
            roleNames.ToList(),
            roleIds,
            user.CreatedAt,
            user.LastLoginAt);
    }

    public async Task<(bool Success, Guid? UserId, string? Error)> CreateUserAsync(
        CreateTenantUserRequest request, CancellationToken ct)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return (false, null, "Bu e-posta zaten kayıtlı.");

        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count > 0)
        {
            var validRoleCount = await _roleManager.Roles
                .CountAsync(r => r.TenantId == request.TenantId && roleIds.Contains(r.Id), ct);

            if (validRoleCount != roleIds.Count)
                return (false, null, "Geçersiz rol seçimi.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return (false, null, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        if (roleIds.Count > 0)
        {
            var roleNames = await _roleManager.Roles
                .Where(r => roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(ct);

            var roleResult = await _userManager.AddToRolesAsync(user, roleNames);
            if (!roleResult.Succeeded)
                return (false, null, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        return (true, user.Id, null);
    }

    public async Task<(bool Success, string? Error)> UpdateUserAsync(
        UpdateTenantUserRequest request, CancellationToken ct)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId && u.TenantId == request.TenantId, ct);

        if (user is null)
            return (false, "Kullanıcı bulunamadı.");

        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)));

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                return (false, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
        }

        var roleIds = request.RoleIds.Distinct().ToList();
        if (roleIds.Count > 0)
        {
            var roleNames = await _roleManager.Roles
                .Where(r => r.TenantId == request.TenantId && roleIds.Contains(r.Id))
                .Select(r => r.Name!)
                .ToListAsync(ct);

            if (roleNames.Count != roleIds.Count)
                return (false, "Geçersiz rol seçimi.");

            var addResult = await _userManager.AddToRolesAsync(user, roleNames);
            if (!addResult.Succeeded)
                return (false, string.Join(", ", addResult.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeactivateUserAsync(
        Guid tenantId, Guid userId, Guid currentUserId, CancellationToken ct)
    {
        if (userId == currentUserId)
            return (false, "Kendi hesabınızı deaktif edemezsiniz.");

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct);

        if (user is null)
            return (false, "Kullanıcı bulunamadı.");

        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }
}
