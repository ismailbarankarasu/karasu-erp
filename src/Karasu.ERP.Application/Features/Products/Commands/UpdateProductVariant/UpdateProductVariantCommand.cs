using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.UpdateProductVariant;

public record UpdateProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string Sku,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    string AttributesJson) : IRequest<Result>;

public class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IProductRepository _productRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductVariantCommandHandler(
        IApplicationDbContext context,
        IProductRepository productRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _productRepository = productRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var productExists = await _context.Products.AnyAsync(
            p => p.Id == request.ProductId && p.TenantId == _tenantContext.TenantId && !p.IsDeleted,
            cancellationToken);

        if (!productExists)
            return Result.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        var variant = await _context.ProductVariants.FirstOrDefaultAsync(
            v => v.Id == request.VariantId &&
                 v.ProductId == request.ProductId &&
                 v.TenantId == _tenantContext.TenantId &&
                 !v.IsDeleted,
            cancellationToken);

        if (variant is null)
            return Result.Failure("Varyant bulunamadı.", "VARIANT_NOT_FOUND");

        if (await _context.ProductVariants.AnyAsync(
                v => v.Sku == request.Sku.Trim() &&
                     v.Id != request.VariantId &&
                     v.TenantId == _tenantContext.TenantId &&
                     !v.IsDeleted,
                cancellationToken) ||
            await _productRepository.SkuExistsAsync(request.Sku, request.ProductId, cancellationToken))
            return Result.Failure("Bu SKU zaten kullanılıyor.", "SKU_EXISTS");

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcodeTaken = await _context.ProductVariants.AnyAsync(
                v => v.Barcode == request.Barcode.Trim() &&
                     v.Id != request.VariantId &&
                     v.TenantId == _tenantContext.TenantId &&
                     !v.IsDeleted,
                cancellationToken);

            if (barcodeTaken)
                return Result.Failure("Bu barkod zaten kullanılıyor.", "BARCODE_EXISTS");

            var existing = await _productRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
            if (existing is not null && existing.Id != request.ProductId)
                return Result.Failure("Bu barkod zaten kullanılıyor.", "BARCODE_EXISTS");
        }

        variant.Sku = request.Sku.Trim();
        variant.Barcode = request.Barcode?.Trim();
        variant.PurchasePrice = request.PurchasePrice;
        variant.SalePrice = request.SalePrice;
        variant.AttributesJson = string.IsNullOrWhiteSpace(request.AttributesJson) ? "{}" : request.AttributesJson;
        variant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
