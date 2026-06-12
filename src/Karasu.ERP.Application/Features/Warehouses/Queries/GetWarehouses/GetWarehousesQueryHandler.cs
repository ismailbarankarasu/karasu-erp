using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouses;

public class GetWarehousesQueryHandler : IRequestHandler<GetWarehousesQuery, Result<List<WarehouseDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetWarehousesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<WarehouseDto>>> Handle(
        GetWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Warehouses
            .AsNoTracking()
            .Where(w => w.TenantId == _tenantContext.TenantId && !w.IsDeleted);

        if (request.BranchId.HasValue)
            query = query.Where(w => w.BranchId == request.BranchId.Value);

        var items = await query
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .Select(w => new WarehouseDto(
                w.Id,
                w.BranchId,
                w.Branch.Name,
                w.Name,
                w.Code,
                w.IsDefault))
            .ToListAsync(cancellationToken);

        return Result<List<WarehouseDto>>.Success(items);
    }
}
