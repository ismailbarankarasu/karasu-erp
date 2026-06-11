using FluentValidation;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Roles.Commands.UpdateRolePermissions;

public record UpdateRolePermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<Result>;

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.PermissionIds).NotNull();
    }
}

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    private readonly IRoleRepository _roles;

    public UpdateRolePermissionsCommandHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken ct)
    {
        var (success, error) = await _roles.UpdateRolePermissionsAsync(
            request.RoleId, request.PermissionIds, ct);

        return success ? Result.Success() : Result.Failure(error ?? "İzinler güncellenemedi.", "ROLE_PERMISSIONS_FAILED");
    }
}
