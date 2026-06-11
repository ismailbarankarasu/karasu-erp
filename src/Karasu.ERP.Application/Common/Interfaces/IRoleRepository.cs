namespace Karasu.ERP.Application.Common.Interfaces;

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct);
    Task<RoleDetailDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct);
    Task<(bool Success, Guid? RoleId, string? Error)> CreateRoleAsync(CreateRoleDto dto, CancellationToken ct);
    Task<(bool Success, string? Error)> UpdateRoleAsync(Guid roleId, UpdateRoleDto dto, CancellationToken ct);
    Task<(bool Success, string? Error)> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken ct);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct);
    Task<IReadOnlyList<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken ct);
}

public record RoleDto(Guid Id, string Name, string? Description, bool IsSystemRole, int PermissionCount);
public record RoleDetailDto(Guid Id, string Name, string? Description, bool IsSystemRole, IReadOnlyList<PermissionDto> Permissions);
public record PermissionDto(Guid Id, string Name, string Module, string Entity, string Action, string Description);
public record CreateRoleDto(string Name, string? Description, IReadOnlyList<Guid>? PermissionIds);
public record UpdateRoleDto(string Name, string? Description);
public record AuditLogDto(Guid Id, Guid? UserId, string EntityType, Guid EntityId, string Action, DateTime CreatedAt, string? IpAddress);
public record AuditLogFilter(int Page = 1, int PageSize = 20, string? EntityType = null, Guid? EntityId = null, Guid? UserId = null);
