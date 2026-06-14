using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.GenerateBarcode;

public record GenerateProductBarcodeCommand(Guid ProductId) : IRequest<Result<GenerateBarcodeResult>>;

public record GenerateBarcodeResult(string Barcode, string ImageBase64);

public class GenerateProductBarcodeCommandHandler : IRequestHandler<GenerateProductBarcodeCommand, Result<GenerateBarcodeResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBarcodeService _barcodeService;

    public GenerateProductBarcodeCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        IBarcodeService barcodeService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _barcodeService = barcodeService;
    }

    public async Task<Result<GenerateBarcodeResult>> Handle(
        GenerateProductBarcodeCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p =>
                p.Id == request.ProductId &&
                p.TenantId == _tenantContext.TenantId &&
                !p.IsDeleted,
                cancellationToken);

        if (product is null)
            return Result<GenerateBarcodeResult>.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        var barcode = string.IsNullOrWhiteSpace(product.Barcode)
            ? _barcodeService.GenerateUniqueBarcode(_tenantContext.TenantId, product.Sku)
            : product.Barcode;

        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            product.Barcode = barcode;
            var defaultVariant = product.Variants.FirstOrDefault(v => !v.IsDeleted);
            if (defaultVariant is not null)
                defaultVariant.Barcode = barcode;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GenerateBarcodeResult>.Success(
            new GenerateBarcodeResult(barcode, _barcodeService.GenerateBarcodeImageBase64(barcode)));
    }
}
