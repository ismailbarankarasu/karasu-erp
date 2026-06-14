using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateProductVariant;

public record CreateProductVariantCommand(
    Guid ProductId,
    string Sku,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    string AttributesJson) : IRequest<Result<Guid>>;

public class CreateProductVariantCommandHandler : IRequestHandler<CreateProductVariantCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IProductRepository _productRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;

    public CreateProductVariantCommandHandler(
        IApplicationDbContext context,
        IProductRepository productRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        IStockService stockService)
    {
        _context = context;
        _productRepository = productRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _stockService = stockService;
    }

    public async Task<Result<Guid>> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null || product.TenantId != _tenantContext.TenantId)
            return Result<Guid>.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        if (await _context.ProductVariants.AnyAsync(
                v => v.Sku == request.Sku.Trim() && v.TenantId == _tenantContext.TenantId && !v.IsDeleted,
                cancellationToken) ||
            await _productRepository.SkuExistsAsync(request.Sku, null, cancellationToken))
            return Result<Guid>.Failure("Bu SKU zaten kullanılıyor.", "SKU_EXISTS");

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcodeTaken = await _context.ProductVariants.AnyAsync(
                v => v.Barcode == request.Barcode.Trim() && v.TenantId == _tenantContext.TenantId && !v.IsDeleted,
                cancellationToken);

            if (barcodeTaken)
                return Result<Guid>.Failure("Bu barkod zaten kullanılıyor.", "BARCODE_EXISTS");

            var existing = await _productRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
            if (existing is not null)
                return Result<Guid>.Failure("Bu barkod zaten kullanılıyor.", "BARCODE_EXISTS");
        }

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ProductId = product.Id,
            Sku = request.Sku.Trim(),
            Barcode = request.Barcode?.Trim(),
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            AttributesJson = string.IsNullOrWhiteSpace(request.AttributesJson) ? "{}" : request.AttributesJson,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ProductVariants.AddAsync(variant, cancellationToken);

        var warehouseId = await _stockService.GetDefaultWarehouseIdAsync(cancellationToken);
        if (warehouseId is null)
            return Result<Guid>.Failure("Varsayılan depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        var stockResult = await _stockService.EnsureStockItemAsync(
            warehouseId.Value, variant.Id, product.MinStock, cancellationToken);
        if (!stockResult.IsSuccess)
            return Result<Guid>.Failure(stockResult.Error!, stockResult.ErrorCode);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(variant.Id);
    }
}
