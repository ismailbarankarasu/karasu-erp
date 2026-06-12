using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockByVariant;

public class GetStockByVariantQueryHandler : IRequestHandler<GetStockByVariantQuery, Result<StockByVariantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockByVariantQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StockByVariantDto>> Handle(
        GetStockByVariantQuery request,
        CancellationToken cancellationToken)
    {
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == request.ProductVariantId &&
                        v.TenantId == _tenantContext.TenantId &&
                        !v.IsDeleted)
            .Select(v => new { v.Id, v.Sku, ProductName = v.Product.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (variant is null)
            return Result<StockByVariantDto>.Failure("Ürün varyantı bulunamadı.", "PRODUCT_VARIANT_NOT_FOUND");

        var warehouses = await _context.StockItems
            .AsNoTracking()
            .Where(s => s.ProductVariantId == request.ProductVariantId && !s.IsDeleted)
            .Select(s => new StockByWarehouseDto(
                s.WarehouseId,
                s.Warehouse.Name,
                s.Quantity,
                s.ReservedQuantity,
                s.Quantity - s.ReservedQuantity,
                s.MinStock))
            .ToListAsync(cancellationToken);

        return Result<StockByVariantDto>.Success(new StockByVariantDto(
            variant.Id,
            variant.ProductName,
            variant.Sku,
            warehouses.Sum(w => w.Quantity),
            warehouses.Sum(w => w.AvailableQuantity),
            warehouses));
    }
}
