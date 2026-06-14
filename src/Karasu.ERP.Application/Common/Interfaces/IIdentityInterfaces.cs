using System.Security.Claims;

namespace Karasu.ERP.Application.Common.Interfaces;

public interface ITokenService
{
    Task<TokenResult> GenerateTokensAsync(AuthUserDto user, CancellationToken ct);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public record TokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt);

public record AuthUserDto(
    Guid Id,
    Guid? TenantId,
    string Email,
    string FullName,
    IList<string> Roles,
    IList<string> Permissions);

public interface IIdentityService
{
    Task<(bool Success, AuthUserDto? User, string? Error)> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct);

    Task<(bool Success, AuthUserDto? User, string? Error)> RegisterTenantAsync(
        string companyName, string slug, string email, string password, string fullName, CancellationToken ct);

    Task<AuthUserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct);
    Task<IList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct);

    Task<(bool Success, string? Error, string? ResetToken)> ForgotPasswordAsync(string email, CancellationToken ct);
    Task<(bool Success, string? Error)> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct);
    Task<(bool Success, AuthUserDto? User, string? Error)> UpdateProfileAsync(
        Guid userId, string fullName, string? email, CancellationToken ct);
    Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct);
}

public interface IUserManagementService
{
    Task<PaginatedUsersResult> GetUsersAsync(Guid tenantId, int page, int pageSize, string? search, CancellationToken ct);
    Task<UserDetailDto?> GetUserAsync(Guid tenantId, Guid userId, CancellationToken ct);
    Task<(bool Success, Guid? UserId, string? Error)> CreateUserAsync(CreateTenantUserRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> UpdateUserAsync(UpdateTenantUserRequest request, CancellationToken ct);
    Task<(bool Success, string? Error)> DeactivateUserAsync(Guid tenantId, Guid userId, Guid currentUserId, CancellationToken ct);
}

public record PaginatedUsersResult(IReadOnlyList<UserListItemDto> Items, int TotalCount, int Page, int PageSize);

public record UserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record UserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> RoleIds,
    DateTime CreatedAt,
    DateTime? LastLoginAt);

public record CreateTenantUserRequest(
    Guid TenantId,
    string Email,
    string Password,
    string FullName,
    IReadOnlyList<Guid> RoleIds);

public record UpdateTenantUserRequest(
    Guid TenantId,
    Guid UserId,
    string FullName,
    bool IsActive,
    IReadOnlyList<Guid> RoleIds);

public interface IRefreshTokenRepository
{
    Task StoreAsync(Guid userId, string token, DateTime expiresAt, string? ip, CancellationToken ct);
    Task<RefreshTokenInfo?> GetByTokenAsync(string token, CancellationToken ct);
    Task RevokeAsync(string token, string? replacedBy, string? ip, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct);
}

public record RefreshTokenInfo(Guid Id, Guid UserId, string Token, DateTime ExpiresAt, bool IsActive);

public interface IDateTimeService
{
    DateTime UtcNow { get; }
}
