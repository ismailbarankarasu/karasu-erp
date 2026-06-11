using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockMovements;

public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, Result<PaginatedList<StockMovementDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockMovementsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<StockMovementDto>>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockMovements
            .AsNoTracking()
            .Where(m => m.TenantId == _tenantContext.TenantId && !m.IsDeleted);

        if (request.WarehouseId.HasValue)
            query = query.Where(m => m.StockItem.WarehouseId == request.WarehouseId.Value);

        if (request.ProductVariantId.HasValue)
            query = query.Where(m => m.StockItem.ProductVariantId == request.ProductVariantId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new StockMovementDto(
                m.Id,
                m.StockItemId,
                m.StockItem.ProductVariant.Product.Name,
                m.StockItem.ProductVariant.Sku,
                m.StockItem.Warehouse.Name,
                m.Type,
                m.Quantity,
                m.ReferenceType,
                m.ReferenceId,
                m.Note,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<StockMovementDto>>.Success(
            new PaginatedList<StockMovementDto>(items, totalCount, request.Page, request.PageSize));
    }
}
