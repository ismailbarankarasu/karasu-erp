using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Users.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid Id) : IRequest<Result>;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUserManagementService _userManagement;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public DeactivateUserCommandHandler(
        IUserManagementService userManagement,
        ITenantContext tenantContext,
        ICurrentUserService currentUser)
    {
        _userManagement = userManagement;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure("Kimlik doğrulama gerekli.", "UNAUTHORIZED");

        var (success, error) = await _userManagement.DeactivateUserAsync(
            _tenantContext.TenantId,
            request.Id,
            _currentUser.UserId.Value,
            cancellationToken);

        return success ? Result.Success() : Result.Failure(error ?? "Kullanıcı deaktif edilemedi.", "USER_DEACTIVATE_FAILED");
    }
}
