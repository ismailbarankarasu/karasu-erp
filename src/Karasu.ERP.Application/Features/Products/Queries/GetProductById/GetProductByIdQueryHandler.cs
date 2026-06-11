using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetProductByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductDetailDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.TenantId == _tenantContext.TenantId && !p.IsDeleted)
            .Select(p => new ProductDetailDto(
                p.Id,
                p.Sku,
                p.Barcode,
                p.Name,
                p.CategoryId,
                p.Category != null ? p.Category.Name : null,
                p.BrandId,
                p.Brand != null ? p.Brand.Name : null,
                p.UnitId,
                p.Unit.Name,
                p.Unit.Symbol,
                p.PurchasePrice,
                p.SalePrice,
                p.TaxRate,
                p.MinStock,
                p.ImageUrl,
                p.Status,
                p.Variants.OrderBy(v => v.CreatedAt).Select(v => (Guid?)v.Id).FirstOrDefault(),
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
            return Result<ProductDetailDto>.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        return Result<ProductDetailDto>.Success(product);
    }
}
