using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Warehouses.Commands.UpdateWarehouse;

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWarehouseCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateWarehouseCommand request,
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

        var codeExists = await _context.Warehouses.AnyAsync(
            w => w.TenantId == _tenantContext.TenantId &&
                 w.Code == request.Code.Trim() &&
                 w.Id != request.Id &&
                 !w.IsDeleted,
            cancellationToken);

        if (codeExists)
            return Result.Failure("Bu depo kodu zaten kullanılıyor.", "WAREHOUSE_CODE_EXISTS");

        if (request.IsDefault && !warehouse.IsDefault)
        {
            var existingDefaults = await _context.Warehouses
                .Where(w => w.BranchId == warehouse.BranchId && w.IsDefault && w.Id != warehouse.Id && !w.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
                existing.IsDefault = false;
        }

        if (!request.IsDefault && warehouse.IsDefault)
            return Result.Failure("Varsayılan depo kaldırılamaz. Önce başka bir depoyu varsayılan yapın.", "WAREHOUSE_DEFAULT_REQUIRED");

        warehouse.Name = request.Name.Trim();
        warehouse.Code = request.Code.Trim().ToUpperInvariant();
        warehouse.IsDefault = request.IsDefault;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
