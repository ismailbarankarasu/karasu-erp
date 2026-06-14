using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<Result<PaginatedList<UserListItemDto>>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginatedList<UserListItemDto>>>
{
    private readonly IUserManagementService _userManagement;
    private readonly ITenantContext _tenantContext;

    public GetUsersQueryHandler(IUserManagementService userManagement, ITenantContext tenantContext)
    {
        _userManagement = userManagement;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<UserListItemDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _userManagement.GetUsersAsync(
            _tenantContext.TenantId,
            request.Page,
            request.PageSize,
            request.Search,
            cancellationToken);

        return Result<PaginatedList<UserListItemDto>>.Success(
            new PaginatedList<UserListItemDto>(result.Items, result.TotalCount, result.Page, result.PageSize));
    }
}
