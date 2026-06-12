using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.CompleteStockTransfer;

public class CompleteStockTransferCommandHandler : IRequestHandler<CompleteStockTransferCommand, Result>
{
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public CompleteStockTransferCommandHandler(
        IStockService stockService,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _stockService = stockService;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result> Handle(
        CompleteStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _stockService.CompleteTransferAsync(request.TransferId, cancellationToken);
        if (!result.IsSuccess)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateStockCacheAsync(cancellationToken);
        return Result.Success();
    }

    private async Task InvalidateStockCacheAsync(CancellationToken cancellationToken)
    {
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:stock:item:*", cancellationToken);
        await _cacheService.RemoveAsync($"{_tenantContext.TenantId}:stock:alerts:critical", cancellationToken);
    }
}
