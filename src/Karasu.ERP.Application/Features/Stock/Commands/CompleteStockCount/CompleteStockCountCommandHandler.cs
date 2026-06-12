using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Stock.Commands.CompleteStockCount;

public class CompleteStockCountCommandHandler : IRequestHandler<CompleteStockCountCommand, Result>
{
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public CompleteStockCountCommandHandler(
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
        CompleteStockCountCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _stockService.CompleteCountAsync(request.CountId, cancellationToken);
        if (!result.IsSuccess)
            return result;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:stock:item:*", cancellationToken);
        await _cacheService.RemoveAsync($"{_tenantContext.TenantId}:stock:alerts:critical", cancellationToken);

        return Result.Success();
    }
}
