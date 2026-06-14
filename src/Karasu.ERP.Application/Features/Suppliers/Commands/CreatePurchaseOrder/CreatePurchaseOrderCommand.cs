using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Commands.CreatePurchaseOrder;

public record CreatePurchaseOrderLineRequest(
    Guid ProductVariantId,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate);

public record CreatePurchaseOrderCommand(
    Guid SupplierId,
    DateTime? ExpectedDate,
    string? Notes,
    IReadOnlyList<CreatePurchaseOrderLineRequest> Lines,
    bool SendImmediately = true) : IRequest<Result<Guid>>;

public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreatePurchaseOrderCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            return Result<Guid>.Failure("En az bir satır gereklidir.", "LINES_REQUIRED");

        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.Id == request.SupplierId &&
                           s.TenantId == _tenantContext.TenantId &&
                           !s.IsDeleted &&
                           s.IsActive, cancellationToken);
        if (!supplierExists)
            return Result<Guid>.Failure("Geçersiz tedarikçi.", "SUPPLIER_NOT_FOUND");

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variantCount = await _context.ProductVariants
            .CountAsync(v => variantIds.Contains(v.Id) &&
                             v.TenantId == _tenantContext.TenantId &&
                             !v.IsDeleted, cancellationToken);
        if (variantCount != variantIds.Count)
            return Result<Guid>.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        var poNumber = await GeneratePoNumberAsync(cancellationToken);
        var po = PurchaseOrder.Create(
            _tenantContext.TenantId,
            request.SupplierId,
            poNumber,
            request.ExpectedDate,
            request.Notes);

        foreach (var line in request.Lines)
        {
            po.AddLine(line.ProductVariantId, line.Quantity, line.UnitPrice, line.TaxRate);
        }

        if (request.SendImmediately)
        {
            try
            {
                po.Send();
            }
            catch (InvalidOperationException ex)
            {
                return Result<Guid>.Failure(ex.Message, "PO_SEND_FAILED");
            }
        }

        _context.PurchaseOrders.Add(po);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(po.Id);
    }

    private async Task<string> GeneratePoNumberAsync(CancellationToken cancellationToken)
    {
        var count = await _context.PurchaseOrders
            .CountAsync(p => p.TenantId == _tenantContext.TenantId, cancellationToken);
        return $"PO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }
}
