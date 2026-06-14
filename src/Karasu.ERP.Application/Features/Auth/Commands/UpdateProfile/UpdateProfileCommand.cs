using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Auth.Commands.Login;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(string FullName, string? Email) : IRequest<Result<AuthUserResponse>>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<AuthUserResponse>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identityService;

    public UpdateProfileCommandHandler(ICurrentUserService currentUser, IIdentityService identityService)
    {
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Result<AuthUserResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result<AuthUserResponse>.Failure("Kimlik doğrulama gerekli.", "UNAUTHORIZED");

        var (success, user, error) = await _identityService.UpdateProfileAsync(
            _currentUser.UserId.Value,
            request.FullName,
            request.Email,
            cancellationToken);

        return success && user is not null
            ? Result<AuthUserResponse>.Success(LoginCommandHandler.MapUser(user))
            : Result<AuthUserResponse>.Failure(error ?? "Profil güncellenemedi.", "PROFILE_UPDATE_FAILED");
    }
}
