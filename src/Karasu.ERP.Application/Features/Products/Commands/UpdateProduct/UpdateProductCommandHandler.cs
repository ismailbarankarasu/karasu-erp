using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null || product.TenantId != _tenantContext.TenantId)
            return Result.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        if (await _productRepository.SkuExistsAsync(request.Sku, request.Id, cancellationToken))
            return Result.Failure("Bu SKU zaten kullanılıyor.", "SKU_EXISTS");

        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var existingBarcode = await _productRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
            if (existingBarcode is not null && existingBarcode.Id != request.Id)
                return Result.Failure("Bu barkod zaten kullanılıyor.", "BARCODE_EXISTS");
        }

        var unitExists = await _context.Units
            .AnyAsync(u => u.Id == request.UnitId && u.TenantId == _tenantContext.TenantId, cancellationToken);
        if (!unitExists)
            return Result.Failure("Geçersiz birim.", "UNIT_NOT_FOUND");

        product.Sku = request.Sku;
        product.Barcode = request.Barcode;
        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.UnitId = request.UnitId;
        product.PurchasePrice = request.PurchasePrice;
        product.SalePrice = request.SalePrice;
        product.TaxRate = request.TaxRate;
        product.MinStock = request.MinStock;
        product.Status = request.Status;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateProductCacheAsync(product.Barcode, cancellationToken);

        return Result.Success();
    }

    private async Task InvalidateProductCacheAsync(string? barcode, CancellationToken ct)
    {
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:product:list:*", ct);
        if (!string.IsNullOrEmpty(barcode))
            await _cacheService.RemoveAsync($"{_tenantContext.TenantId}:product:barcode:{barcode}", ct);
    }
}
