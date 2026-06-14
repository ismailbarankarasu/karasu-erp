using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid Id,
    string FullName,
    bool IsActive,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserManagementService _userManagement;
    private readonly ITenantContext _tenantContext;

    public UpdateUserCommandHandler(IUserManagementService userManagement, ITenantContext tenantContext)
    {
        _userManagement = userManagement;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var (success, error) = await _userManagement.UpdateUserAsync(
            new UpdateTenantUserRequest(
                _tenantContext.TenantId,
                request.Id,
                request.FullName,
                request.IsActive,
                request.RoleIds),
            cancellationToken);

        return success ? Result.Success() : Result.Failure(error ?? "Kullanıcı güncellenemedi.", "USER_UPDATE_FAILED");
    }
}
