using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(IIdentityService identityService, ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (success, user, error) = await _identityService.RegisterTenantAsync(
            request.CompanyName,
            request.Slug,
            request.Email,
            request.Password,
            request.FullName,
            cancellationToken);

        if (!success || user is null)
            return Result<AuthResponse>.Failure(error ?? "Kayıt başarısız.", "REGISTER_FAILED");

        var tokens = await _tokenService.GenerateTokensAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            LoginCommandHandler.MapUser(user)));
    }
}
