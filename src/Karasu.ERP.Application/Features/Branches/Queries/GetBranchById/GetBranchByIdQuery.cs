using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Orders.Queries.GetBranches;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Branches.Queries.GetBranchById;

public record GetBranchByIdQuery(Guid Id) : IRequest<Result<BranchDto>>;

public class GetBranchByIdQueryHandler : IRequestHandler<GetBranchByIdQuery, Result<BranchDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetBranchByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<BranchDto>> Handle(GetBranchByIdQuery request, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .Where(b => b.Id == request.Id && b.TenantId == _tenantContext.TenantId && !b.IsDeleted)
            .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return branch is null
            ? Result<BranchDto>.Failure("Şube bulunamadı.", "BRANCH_NOT_FOUND")
            : Result<BranchDto>.Success(branch);
    }
}
