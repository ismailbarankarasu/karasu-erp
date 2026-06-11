using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IIdentityService identityService, ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var (success, user, error) = await _identityService.ValidateCredentialsAsync(
            request.Email, request.Password, cancellationToken);

        if (!success || user is null)
            return Result<AuthResponse>.Failure(error ?? "Giriş başarısız.", "AUTH_FAILED");

        var tokens = await _tokenService.GenerateTokensAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            MapUser(user)));
    }

    internal static AuthUserResponse MapUser(AuthUserDto user) => new(
        user.Id, user.TenantId, user.Email, user.FullName, user.Roles, user.Permissions);
}
