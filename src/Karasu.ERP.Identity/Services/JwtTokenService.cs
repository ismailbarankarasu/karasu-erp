using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Identity.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Karasu.ERP.Identity.Services;

public class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeService _dateTime;

    public JwtTokenService(
        IOptions<JwtSettings> settings,
        IRefreshTokenRepository refreshTokens,
        IDateTimeService dateTime)
    {
        _settings = settings.Value;
        _refreshTokens = refreshTokens;
        _dateTime = dateTime;
    }

    public async Task<TokenResult> GenerateTokensAsync(AuthUserDto user, CancellationToken ct)
    {
        var accessExpires = _dateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);
        var refreshExpires = _dateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

        var accessToken = BuildAccessToken(user, accessExpires);
        var refreshToken = GenerateRefreshToken();

        await _refreshTokens.StoreAsync(user.Id, refreshToken, refreshExpires, null, ct);

        return new TokenResult(accessToken, refreshToken, accessExpires, refreshExpires);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret)),
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private string BuildAccessToken(AuthUserDto user, DateTime expires)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("full_name", user.FullName)
        };

        if (user.TenantId.HasValue)
            claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));

        foreach (var role in user.Roles)
        {
            var roleClaim = role.StartsWith("CompanyOwner_", StringComparison.OrdinalIgnoreCase)
                ? "CompanyOwner"
                : role;
            claims.Add(new Claim(ClaimTypes.Role, roleClaim));
        }

        foreach (var permission in user.Permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
