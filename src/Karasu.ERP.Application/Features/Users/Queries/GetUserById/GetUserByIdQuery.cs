using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDetailDto>>;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly IUserManagementService _userManagement;
    private readonly ITenantContext _tenantContext;

    public GetUserByIdQueryHandler(IUserManagementService userManagement, ITenantContext tenantContext)
    {
        _userManagement = userManagement;
        _tenantContext = tenantContext;
    }

    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManagement.GetUserAsync(_tenantContext.TenantId, request.Id, cancellationToken);
        return user is null
            ? Result<UserDetailDto>.Failure("Kullanıcı bulunamadı.", "USER_NOT_FOUND")
            : Result<UserDetailDto>.Success(user);
    }
}
