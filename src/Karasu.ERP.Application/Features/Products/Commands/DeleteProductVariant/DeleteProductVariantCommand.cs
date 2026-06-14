using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Commands.DeleteProductVariant;

public record DeleteProductVariantCommand(Guid ProductId, Guid VariantId) : IRequest<Result>;

public class DeleteProductVariantCommandHandler : IRequestHandler<DeleteProductVariantCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductVariantCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variantCount = await _context.ProductVariants.CountAsync(
            v => v.ProductId == request.ProductId &&
                 v.TenantId == _tenantContext.TenantId &&
                 !v.IsDeleted,
            cancellationToken);

        if (variantCount <= 1)
            return Result.Failure("Ürünün son varyantı silinemez.", "LAST_VARIANT");

        var variant = await _context.ProductVariants.FirstOrDefaultAsync(
            v => v.Id == request.VariantId &&
                 v.ProductId == request.ProductId &&
                 v.TenantId == _tenantContext.TenantId &&
                 !v.IsDeleted,
            cancellationToken);

        if (variant is null)
            return Result.Failure("Varyant bulunamadı.", "VARIANT_NOT_FOUND");

        variant.IsDeleted = true;
        variant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
