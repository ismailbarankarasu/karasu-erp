using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouseById;

public class GetWarehouseByIdQueryHandler : IRequestHandler<GetWarehouseByIdQuery, Result<WarehouseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetWarehouseByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<WarehouseDto>> Handle(
        GetWarehouseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == request.Id && w.TenantId == _tenantContext.TenantId && !w.IsDeleted)
            .Select(w => new WarehouseDto(
                w.Id,
                w.BranchId,
                w.Branch.Name,
                w.Name,
                w.Code,
                w.IsDefault))
            .FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null)
            return Result<WarehouseDto>.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        return Result<WarehouseDto>.Success(warehouse);
    }
}
