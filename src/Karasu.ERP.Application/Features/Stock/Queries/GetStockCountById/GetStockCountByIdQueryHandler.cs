using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockCountById;

public class GetStockCountByIdQueryHandler : IRequestHandler<GetStockCountByIdQuery, Result<StockCountDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockCountByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<StockCountDetailDto>> Handle(
        GetStockCountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var count = await _context.StockCounts
            .AsNoTracking()
            .Where(c => c.Id == request.Id && c.TenantId == _tenantContext.TenantId && !c.IsDeleted)
            .Select(c => new StockCountDetailDto(
                c.Id,
                c.WarehouseId,
                c.Warehouse.Name,
                c.Status,
                c.CountedBy,
                c.CreatedAt,
                c.CompletedAt,
                c.Note,
                c.Lines.Select(l => new StockCountLineDto(
                    l.Id,
                    l.ProductVariantId,
                    l.ProductVariant.Product.Name,
                    l.ProductVariant.Sku,
                    l.SystemQty,
                    l.CountedQty,
                    (l.CountedQty ?? l.SystemQty) - l.SystemQty)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (count is null)
            return Result<StockCountDetailDto>.Failure("Sayım bulunamadı.", "COUNT_NOT_FOUND");

        return Result<StockCountDetailDto>.Success(count);
    }
}
