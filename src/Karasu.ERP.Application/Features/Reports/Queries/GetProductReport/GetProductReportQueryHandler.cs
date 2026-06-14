using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetProductReport;

public class GetProductReportQueryHandler : IRequestHandler<GetProductReportQuery, Result<ProductReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetProductReportQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductReportDto>> Handle(
        GetProductReportQuery request,
        CancellationToken cancellationToken)
    {
        var dateRange = ReportQueryHelper.ValidateDateRange(request.FromDate, request.ToDate);
        if (!dateRange.IsSuccess)
            return Result.Failure<ProductReportDto>(dateRange.Error!, dateRange.ErrorCode);

        var tenantId = _tenantContext.TenantId;
        var (start, endExclusive) = dateRange.Data!;

        var items = await _context.OrderLines
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId &&
                        !l.IsDeleted &&
                        !l.Order.IsDeleted &&
                        ReportQueryHelper.SalesOrderStatuses.Contains(l.Order.Status) &&
                        l.Order.CreatedAt >= start &&
                        l.Order.CreatedAt < endExclusive)
            .GroupBy(l => new
            {
                l.ProductVariant.Product.Name,
                l.ProductVariant.Sku
            })
            .Select(g => new ProductReportItemDto(
                g.Key.Name,
                g.Key.Sku,
                g.Sum(x => x.Quantity),
                g.Sum(x => x.LineTotal)))
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(cancellationToken);

        return Result<ProductReportDto>.Success(new ProductReportDto(items));
    }
}
