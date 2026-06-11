using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IProductRepository _productRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;
    private readonly IStockService _stockService;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICacheService cacheService,
        IStockService stockService)
    {
        _productRepository = productRepository;
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
        _stockService = stockService;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await _productRepository.SkuExistsAsync(request.Sku, null, cancellationToken))
            return Result<Guid>.Failure("Bu SKU zaten kullanılıyor.", "SKU_EXISTS");

        var unitExists = await _context.Units
            .AnyAsync(u => u.Id == request.UnitId && u.TenantId == _tenantContext.TenantId, cancellationToken);
        if (!unitExists)
            return Result<Guid>.Failure("Geçersiz birim.", "UNIT_NOT_FOUND");

        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var existing = await _productRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
            if (existing is not null)
                return Result<Guid>.Failure("Bu barkod zaten kullanılıyor.", "BARCODE_EXISTS");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Sku = request.Sku,
            Barcode = request.Barcode,
            Name = request.Name,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            UnitId = request.UnitId,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            TaxRate = request.TaxRate,
            MinStock = request.MinStock,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var defaultVariant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ProductId = product.Id,
            Sku = product.Sku,
            Barcode = product.Barcode,
            PurchasePrice = product.PurchasePrice,
            SalePrice = product.SalePrice,
            AttributesJson = "{}",
            CreatedAt = DateTime.UtcNow
        };

        var warehouseId = await _stockService.GetDefaultWarehouseIdAsync(cancellationToken);
        if (warehouseId is null)
            return Result<Guid>.Failure("Varsayılan depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        await _productRepository.AddAsync(product, cancellationToken);
        await _context.ProductVariants.AddAsync(defaultVariant, cancellationToken);

        var stockResult = await _stockService.EnsureStockItemAsync(
            warehouseId.Value, defaultVariant.Id, request.MinStock, cancellationToken);
        if (!stockResult.IsSuccess)
            return Result<Guid>.Failure(stockResult.Error!, stockResult.ErrorCode);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(
            $"{_tenantContext.TenantId}:product:list:*",
            cancellationToken);

        return Result<Guid>.Success(product.Id);
    }
}
