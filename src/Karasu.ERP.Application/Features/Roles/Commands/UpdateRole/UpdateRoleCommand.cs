using FluentValidation;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : IRequest<Result>;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
{
    private readonly IRoleRepository _roles;

    public UpdateRoleCommandHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken ct)
    {
        var (success, error) = await _roles.UpdateRoleAsync(
            request.RoleId, new UpdateRoleDto(request.Name, request.Description), ct);

        return success ? Result.Success() : Result.Failure(error ?? "Rol güncellenemedi.", "ROLE_UPDATE_FAILED");
    }
}
