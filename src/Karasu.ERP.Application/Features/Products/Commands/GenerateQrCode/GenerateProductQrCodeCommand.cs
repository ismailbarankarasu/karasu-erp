using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.GenerateQrCode;

public record GenerateProductQrCodeCommand(Guid ProductId) : IRequest<Result<GenerateQrCodeResult>>;

public record GenerateQrCodeResult(string Content, string ImageBase64);

public class GenerateProductQrCodeCommandHandler : IRequestHandler<GenerateProductQrCodeCommand, Result<GenerateQrCodeResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IBarcodeService _barcodeService;

    public GenerateProductQrCodeCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IBarcodeService barcodeService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _barcodeService = barcodeService;
    }

    public async Task<Result<GenerateQrCodeResult>> Handle(
        GenerateProductQrCodeCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == request.ProductId &&
                p.TenantId == _tenantContext.TenantId &&
                !p.IsDeleted,
                cancellationToken);

        if (product is null)
            return Result<GenerateQrCodeResult>.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        var content = $"SKU:{product.Sku}|BARCODE:{product.Barcode ?? "-"}|ID:{product.Id}";
        return Result<GenerateQrCodeResult>.Success(
            new GenerateQrCodeResult(content, _barcodeService.GenerateQrCodeBase64(content)));
    }
}
