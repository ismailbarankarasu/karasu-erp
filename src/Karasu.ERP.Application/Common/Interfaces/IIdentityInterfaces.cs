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
}

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
