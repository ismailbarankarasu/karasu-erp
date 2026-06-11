using FluentValidation;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name, string? Description, List<Guid>? PermissionIds)
    : IRequest<Result<Guid>>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IRoleRepository _roles;

    public CreateRoleCommandHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var (success, roleId, error) = await _roles.CreateRoleAsync(
            new CreateRoleDto(request.Name, request.Description, request.PermissionIds), ct);

        return success && roleId.HasValue
            ? Result<Guid>.Success(roleId.Value)
            : Result<Guid>.Failure(error ?? "Rol oluşturulamadı.", "ROLE_CREATE_FAILED");
    }
}
