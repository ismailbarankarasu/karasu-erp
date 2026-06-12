using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Warehouses.Commands.DeleteWarehouse;

public class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWarehouseCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(
                w => w.Id == request.Id &&
                     w.TenantId == _tenantContext.TenantId &&
                     !w.IsDeleted,
                cancellationToken);

        if (warehouse is null)
            return Result.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        if (warehouse.IsDefault)
            return Result.Failure("Varsayılan depo silinemez.", "WAREHOUSE_IS_DEFAULT");

        var hasStock = await _context.StockItems.AnyAsync(
            s => s.WarehouseId == warehouse.Id && s.Quantity > 0 && !s.IsDeleted,
            cancellationToken);

        if (hasStock)
            return Result.Failure("Stok bulunan depo silinemez.", "WAREHOUSE_HAS_STOCK");

        var pendingTransfer = await _context.StockTransfers.AnyAsync(
            t => t.Status == Domain.Enums.StockTransferStatus.Pending &&
                 (t.FromWarehouseId == warehouse.Id || t.ToWarehouseId == warehouse.Id) &&
                 !t.IsDeleted,
            cancellationToken);

        if (pendingTransfer)
            return Result.Failure("Bekleyen transferi olan depo silinemez.", "WAREHOUSE_HAS_PENDING_TRANSFER");

        warehouse.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
