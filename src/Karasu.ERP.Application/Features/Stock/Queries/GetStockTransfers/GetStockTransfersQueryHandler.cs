using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Queries.GetStockTransfers;

public class GetStockTransfersQueryHandler : IRequestHandler<GetStockTransfersQuery, Result<PaginatedList<StockTransferDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetStockTransfersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<StockTransferDto>>> Handle(
        GetStockTransfersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.StockTransfers
            .AsNoTracking()
            .Where(t => t.TenantId == _tenantContext.TenantId && !t.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(t => t.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new StockTransferDto(
                t.Id,
                t.FromWarehouseId,
                t.FromWarehouse.Name,
                t.ToWarehouseId,
                t.ToWarehouse.Name,
                t.Status,
                t.RequestedBy,
                t.CreatedAt,
                t.CompletedAt,
                t.Lines.Count))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<StockTransferDto>>.Success(
            new PaginatedList<StockTransferDto>(items, totalCount, request.Page, request.PageSize));
    }
}
