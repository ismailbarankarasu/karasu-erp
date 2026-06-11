using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null || product.TenantId != _tenantContext.TenantId)
            return Result.Failure("Ürün bulunamadı.", "PRODUCT_NOT_FOUND");

        product.IsDeleted = true;
        product.Status = ProductStatus.Inactive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:product:list:*", cancellationToken);
        if (!string.IsNullOrEmpty(product.Barcode))
            await _cacheService.RemoveAsync($"{_tenantContext.TenantId}:product:barcode:{product.Barcode}", cancellationToken);

        return Result.Success();
    }
}
