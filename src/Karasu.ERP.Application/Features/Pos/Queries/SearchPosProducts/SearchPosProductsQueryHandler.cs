using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Queries.SearchPosProducts;

public class SearchPosProductsQueryHandler : IRequestHandler<SearchPosProductsQuery, Result<IReadOnlyList<PosProductDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public SearchPosProductsQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IStockService stockService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<Result<IReadOnlyList<PosProductDto>>> Handle(
        SearchPosProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcode = request.Barcode.Trim();
            query = query.Where(p => p.Barcode == barcode);
        }
        else if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Sku.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)));
        }
        else
        {
            return Result<IReadOnlyList<PosProductDto>>.Failure(
                "Barkod veya arama terimi gereklidir.",
                "SEARCH_TERM_REQUIRED");
        }

        var warehouseId = await _stockService.GetDefaultWarehouseIdAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Take(20)
            .Select(p => new PosProductDto(
                p.Id,
                p.Variants.OrderBy(v => v.CreatedAt).Select(v => (Guid?)v.Id).FirstOrDefault(),
                p.Sku,
                p.Barcode,
                p.Name,
                p.SalePrice,
                p.TaxRate,
                null))
            .ToListAsync(cancellationToken);

        if (warehouseId.HasValue)
        {
            var variantIds = items.Where(i => i.VariantId.HasValue).Select(i => i.VariantId!.Value).ToList();
            var stockMap = await _context.StockItems
                .AsNoTracking()
                .Where(s => s.WarehouseId == warehouseId.Value && variantIds.Contains(s.ProductVariantId))
                .ToDictionaryAsync(s => s.ProductVariantId, s => s.Quantity - s.ReservedQuantity, cancellationToken);

            items = items.Select(i => i with
            {
                AvailableStock = i.VariantId.HasValue && stockMap.TryGetValue(i.VariantId.Value, out var qty)
                    ? qty
                    : 0m
            }).ToList();
        }

        return Result<IReadOnlyList<PosProductDto>>.Success(items);
    }
}
