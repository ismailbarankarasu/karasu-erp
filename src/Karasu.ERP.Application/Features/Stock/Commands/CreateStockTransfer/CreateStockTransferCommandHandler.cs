using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Commands.CreateStockTransfer;

public class CreateStockTransferCommandHandler : IRequestHandler<CreateStockTransferCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStockTransferCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
            return Result<Guid>.Failure("Kaynak ve hedef depo aynı olamaz.", "TRANSFER_SAME_WAREHOUSE");

        var warehouseIds = new[] { request.FromWarehouseId, request.ToWarehouseId };
        var warehouseCount = await _context.Warehouses.CountAsync(
            w => warehouseIds.Contains(w.Id) &&
                 w.TenantId == _tenantContext.TenantId &&
                 !w.IsDeleted,
            cancellationToken);

        if (warehouseCount != 2)
            return Result<Guid>.Failure("Geçersiz depo.", "WAREHOUSE_NOT_FOUND");

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variantCount = await _context.ProductVariants.CountAsync(
            v => variantIds.Contains(v.Id) &&
                 v.TenantId == _tenantContext.TenantId &&
                 !v.IsDeleted,
            cancellationToken);

        if (variantCount != variantIds.Count)
            return Result<Guid>.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        var transfer = new StockTransfer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            Status = StockTransferStatus.Pending,
            RequestedBy = _currentUser.UserId ?? Guid.Empty,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var line in request.Lines)
        {
            await _context.StockTransferLines.AddAsync(new StockTransferLine
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                TransferId = transfer.Id,
                ProductVariantId = line.ProductVariantId,
                Quantity = line.Quantity,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _context.StockTransfers.AddAsync(transfer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(transfer.Id);
    }
}
