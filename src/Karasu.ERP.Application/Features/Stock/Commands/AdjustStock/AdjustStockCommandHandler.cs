using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Commands.AdjustStock;

public partial class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Result>
{
    private readonly IStockService _stockService;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;
    private readonly IOutboxService _outboxService;

    public AdjustStockCommandHandler(
        IStockService stockService,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICacheService cacheService,
        IOutboxService outboxService)
    {
        _stockService = stockService;
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
        _outboxService = outboxService;
    }

    public async Task<Result> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w =>
                w.Id == request.WarehouseId &&
                w.TenantId == _tenantContext.TenantId &&
                !w.IsDeleted,
                cancellationToken);

        if (!warehouseExists)
            return Result.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        var result = await _stockService.AdjustStockAsync(
            request.WarehouseId,
            request.ProductVariantId,
            request.QuantityDelta,
            request.Note,
            cancellationToken);

        if (!result.IsSuccess)
            return result;

        await CheckCriticalStockAlertAsync(request.WarehouseId, request.ProductVariantId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(
            $"{_tenantContext.TenantId}:stock:item:*",
            cancellationToken);
        await _cacheService.RemoveAsync($"{_tenantContext.TenantId}:stock:alerts:critical", cancellationToken);

        return Result.Success();
    }
}
