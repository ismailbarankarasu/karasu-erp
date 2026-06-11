using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Queries.GetBranches;

public class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, Result<IReadOnlyList<BranchDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetBranchesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<BranchDto>>> Handle(
        GetBranchesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.Branches
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted && b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BranchDto(b.Id, b.Name, b.Code, b.IsActive))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BranchDto>>.Success(items);
    }
}
