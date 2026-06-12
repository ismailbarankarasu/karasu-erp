using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public UpdateOrderCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(
                o => o.Id == request.Id && o.TenantId == _tenantContext.TenantId && !o.IsDeleted,
                cancellationToken);

        if (order is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Status != OrderStatus.Draft)
            return Result.Failure("Sadece taslak siparişler güncellenebilir.", "ORDER_INVALID_STATUS");

        if (request.CustomerId.HasValue)
        {
            var customerExists = await _context.Customers.AnyAsync(
                c => c.Id == request.CustomerId.Value && c.TenantId == _tenantContext.TenantId && !c.IsDeleted,
                cancellationToken);
            if (!customerExists)
                return Result.Failure("Geçersiz müşteri.", "CUSTOMER_NOT_FOUND");
        }

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variantCount = await _context.ProductVariants.CountAsync(
            v => variantIds.Contains(v.Id) && v.TenantId == _tenantContext.TenantId && !v.IsDeleted,
            cancellationToken);
        if (variantCount != variantIds.Count)
            return Result.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        try
        {
            order.UpdateDraft(
                request.CustomerId,
                request.Notes,
                request.Lines.Select(l => (l.ProductVariantId, l.Quantity, l.UnitPrice, l.TaxRate, l.Discount)));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "ORDER_INVALID_STATUS");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
