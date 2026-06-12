using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Warehouses.Commands.CreateWarehouse;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWarehouseCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        var branchExists = await _context.Branches.AnyAsync(
            b => b.Id == request.BranchId &&
                 b.TenantId == _tenantContext.TenantId &&
                 !b.IsDeleted,
            cancellationToken);

        if (!branchExists)
            return Result<Guid>.Failure("Şube bulunamadı.", "BRANCH_NOT_FOUND");

        var codeExists = await _context.Warehouses.AnyAsync(
            w => w.TenantId == _tenantContext.TenantId &&
                 w.Code == request.Code.Trim() &&
                 !w.IsDeleted,
            cancellationToken);

        if (codeExists)
            return Result<Guid>.Failure("Bu depo kodu zaten kullanılıyor.", "WAREHOUSE_CODE_EXISTS");

        if (request.IsDefault)
        {
            var existingDefaults = await _context.Warehouses
                .Where(w => w.BranchId == request.BranchId && w.IsDefault && !w.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingDefaults)
                existing.IsDefault = false;
        }

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            IsDefault = request.IsDefault,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Warehouses.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(warehouse.Id);
    }
}
