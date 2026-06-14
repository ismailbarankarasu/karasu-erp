using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string Password,
    string FullName,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<Guid>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IUserManagementService _userManagement;
    private readonly ITenantContext _tenantContext;

    public CreateUserCommandHandler(IUserManagementService userManagement, ITenantContext tenantContext)
    {
        _userManagement = userManagement;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var (success, userId, error) = await _userManagement.CreateUserAsync(
            new CreateTenantUserRequest(
                _tenantContext.TenantId,
                request.Email,
                request.Password,
                request.FullName,
                request.RoleIds),
            cancellationToken);

        return success && userId.HasValue
            ? Result<Guid>.Success(userId.Value)
            : Result<Guid>.Failure(error ?? "Kullanıcı oluşturulamadı.", "USER_CREATE_FAILED");
    }
}
