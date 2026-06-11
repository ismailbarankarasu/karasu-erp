using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Permissions.Queries.GetPermissions;

public record GetPermissionsQuery : IRequest<Result<IReadOnlyList<PermissionDto>>>;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<IReadOnlyList<PermissionDto>>>
{
    private readonly IRoleRepository _roles;

    public GetPermissionsQueryHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<PermissionDto>>.Success(await _roles.GetPermissionsAsync(ct));
}
