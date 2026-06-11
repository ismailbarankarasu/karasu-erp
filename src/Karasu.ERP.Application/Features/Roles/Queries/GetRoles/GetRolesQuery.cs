using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Roles.Queries.GetRoles;

public record GetRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IRoleRepository _roles;

    public GetRolesQueryHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<RoleDto>>.Success(await _roles.GetRolesAsync(ct));
}
