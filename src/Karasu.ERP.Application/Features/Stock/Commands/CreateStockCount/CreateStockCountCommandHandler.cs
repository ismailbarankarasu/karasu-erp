using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Commands.CreateStockCount;

public class CreateStockCountCommandHandler : IRequestHandler<CreateStockCountCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStockCountCommandHandler(
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
        CreateStockCountCommand request,
        CancellationToken cancellationToken)
    {
        var warehouseExists = await _context.Warehouses.AnyAsync(
            w => w.Id == request.WarehouseId &&
                 w.TenantId == _tenantContext.TenantId &&
                 !w.IsDeleted,
            cancellationToken);

        if (!warehouseExists)
            return Result<Guid>.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        var activeCountExists = await _context.StockCounts.AnyAsync(
            c => c.WarehouseId == request.WarehouseId &&
                 c.Status == StockCountStatus.InProgress &&
                 !c.IsDeleted,
            cancellationToken);

        if (activeCountExists)
            return Result<Guid>.Failure("Bu depoda devam eden bir sayım zaten var.", "COUNT_ALREADY_IN_PROGRESS");

        var stockItems = await _context.StockItems
            .AsNoTracking()
            .Where(s => s.WarehouseId == request.WarehouseId && !s.IsDeleted)
            .Select(s => new { s.ProductVariantId, s.Quantity })
            .ToListAsync(cancellationToken);

        var count = new StockCount
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            WarehouseId = request.WarehouseId,
            Status = StockCountStatus.InProgress,
            CountedBy = _currentUser.UserId ?? Guid.Empty,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in stockItems)
        {
            await _context.StockCountLines.AddAsync(new StockCountLine
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                CountId = count.Id,
                ProductVariantId = item.ProductVariantId,
                SystemQty = item.Quantity,
                CountedQty = null,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _context.StockCounts.AddAsync(count, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(count.Id);
    }
}
