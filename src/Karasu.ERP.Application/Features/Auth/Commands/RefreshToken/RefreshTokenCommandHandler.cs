using System.Security.Claims;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IIdentityService identityService)
    {
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _identityService = identityService;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
            return Result<AuthResponse>.Failure("Geçersiz access token.", "INVALID_TOKEN");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Result<AuthResponse>.Failure("Geçersiz token.", "INVALID_TOKEN");

        var storedToken = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive || storedToken.UserId != userId)
            return Result<AuthResponse>.Failure("Geçersiz refresh token.", "INVALID_REFRESH_TOKEN");

        var user = await _identityService.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
            return Result<AuthResponse>.Failure("Kullanıcı bulunamadı.", "USER_NOT_FOUND");

        var tokens = await _tokenService.GenerateTokensAsync(user, cancellationToken);
        await _refreshTokens.RevokeAsync(request.RefreshToken, tokens.RefreshToken, null, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            LoginCommandHandler.MapUser(user)));
    }
}
