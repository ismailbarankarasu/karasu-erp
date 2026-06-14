using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetStockReport;

public class GetStockReportQueryHandler : IRequestHandler<GetStockReportQuery, Result<StockReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockReportQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StockReportDto>> Handle(
        GetStockReportQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockItems
            .AsNoTracking()
            .Where(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        var items = await query
            .OrderBy(s => s.Warehouse.Name)
            .ThenBy(s => s.ProductVariant.Product.Name)
            .Select(s => new StockReportItemDto(
                s.Warehouse.Name,
                s.ProductVariant.Product.Name,
                s.ProductVariant.Sku,
                s.Quantity,
                s.MinStock,
                s.Quantity - s.ReservedQuantity))
            .ToListAsync(cancellationToken);

        return Result<StockReportDto>.Success(new StockReportDto(items));
    }
}
