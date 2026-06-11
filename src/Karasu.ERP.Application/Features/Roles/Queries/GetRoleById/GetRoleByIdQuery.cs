using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Roles.Queries.GetRoleById;

public record GetRoleByIdQuery(Guid RoleId) : IRequest<Result<RoleDetailDto>>;

public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Result<RoleDetailDto>>
{
    private readonly IRoleRepository _roles;

    public GetRoleByIdQueryHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result<RoleDetailDto>> Handle(GetRoleByIdQuery request, CancellationToken ct)
    {
        var role = await _roles.GetRoleByIdAsync(request.RoleId, ct);
        return role is null
            ? Result<RoleDetailDto>.Failure("Rol bulunamadı.", "ROLE_NOT_FOUND")
            : Result<RoleDetailDto>.Success(role);
    }
}
