using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    AuthUserResponse User);

public record AuthUserResponse(
    Guid Id,
    Guid? TenantId,
    string Email,
    string FullName,
    IList<string> Roles,
    IList<string> Permissions);
