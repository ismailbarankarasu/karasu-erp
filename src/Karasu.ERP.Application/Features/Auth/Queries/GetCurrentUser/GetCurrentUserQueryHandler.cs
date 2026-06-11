using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<AuthUserResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(ICurrentUserService currentUser, IIdentityService identityService)
    {
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Result<AuthUserResponse>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<AuthUserResponse>.Failure("Kimlik doğrulanmadı.", "UNAUTHORIZED");

        var user = await _identityService.GetUserByIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (user is null)
            return Result<AuthUserResponse>.Failure("Kullanıcı bulunamadı.", "USER_NOT_FOUND");

        return Result<AuthUserResponse>.Success(LoginCommandHandler.MapUser(user));
    }
}
