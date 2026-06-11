using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Queries.GetProductByBarcode;

/// <summary>
/// POS barkod araması — Redis cache-aside ile &lt;10ms hedef.
/// </summary>
public class GetProductByBarcodeQueryHandler : IRequestHandler<GetProductByBarcodeQuery, Result<ProductBarcodeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly ITenantContext _tenantContext;

    public GetProductByBarcodeQueryHandler(
        IApplicationDbContext context,
        ICacheService cacheService,
        ITenantContext tenantContext)
    {
        _context = context;
        _cacheService = cacheService;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductBarcodeDto>> Handle(
        GetProductByBarcodeQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{_tenantContext.TenantId}:product:barcode:{request.Barcode}";

        var dto = await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(p => p.Barcode == request.Barcode && p.TenantId == _tenantContext.TenantId && !p.IsDeleted)
                .Select(p => new ProductBarcodeDto(
                    p.Id,
                    p.Variants.OrderBy(v => v.CreatedAt).Select(v => (Guid?)v.Id).FirstOrDefault(),
                    p.Sku,
                    p.Name,
                    p.SalePrice,
                    p.TaxRate,
                    p.ImageUrl))
                .FirstOrDefaultAsync(cancellationToken);

            return product!;
        }, TimeSpan.FromMinutes(15), cancellationToken);

        if (dto is null || dto.Id == Guid.Empty)
            return Result<ProductBarcodeDto>.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        return Result<ProductBarcodeDto>.Success(dto);
    }
}
