using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens)
    {
        _refreshTokens = refreshTokens;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokens.RevokeAsync(request.RefreshToken, null, null, cancellationToken);
        return Result.Success();
    }
}
