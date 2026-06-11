using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockItems;

public class GetStockItemsQueryHandler : IRequestHandler<GetStockItemsQuery, Result<PaginatedList<StockItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockItemsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<StockItemDto>>> Handle(
        GetStockItemsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockItems
            .AsNoTracking()
            .Where(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s =>
                s.ProductVariant.Product.Name.Contains(term) ||
                s.ProductVariant.Sku.Contains(term) ||
                (s.ProductVariant.Barcode != null && s.ProductVariant.Barcode.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.ProductVariant.Product.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StockItemDto(
                s.Id,
                s.WarehouseId,
                s.Warehouse.Name,
                s.ProductVariantId,
                s.ProductVariant.Product.Name,
                s.ProductVariant.Sku,
                s.Quantity,
                s.ReservedQuantity,
                s.Quantity - s.ReservedQuantity,
                s.MinStock))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<StockItemDto>>.Success(
            new PaginatedList<StockItemDto>(items, totalCount, request.Page, request.PageSize));
    }
}
