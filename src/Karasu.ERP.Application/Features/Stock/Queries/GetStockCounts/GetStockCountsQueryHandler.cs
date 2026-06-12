using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockCounts;

public class GetStockCountsQueryHandler : IRequestHandler<GetStockCountsQuery, Result<PaginatedList<StockCountListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockCountsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<StockCountListDto>>> Handle(
        GetStockCountsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockCounts
            .AsNoTracking()
            .Where(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);

        if (request.WarehouseId.HasValue)
            query = query.Where(c => c.WarehouseId == request.WarehouseId.Value);

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new StockCountListDto(
                c.Id,
                c.WarehouseId,
                c.Warehouse.Name,
                c.Status,
                c.CountedBy,
                c.CreatedAt,
                c.CompletedAt,
                c.Lines.Count))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<StockCountListDto>>.Success(
            new PaginatedList<StockCountListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
