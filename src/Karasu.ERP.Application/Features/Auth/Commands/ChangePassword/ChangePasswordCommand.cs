using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(ICurrentUserService currentUser, IIdentityService identityService)
    {
        _currentUser = currentUser;
        _identityService = identityService;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("Kimlik doğrulama gerekli.", "UNAUTHORIZED");

        var (success, error) = await _identityService.ChangePasswordAsync(
            _currentUser.UserId.Value,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return success ? Result.Success() : Result.Failure(error ?? "Şifre değiştirilemedi.", "CHANGE_PASSWORD_FAILED");
    }
}
