using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetSalesReport;

public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, Result<SalesReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetSalesReportQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SalesReportDto>> Handle(
        GetSalesReportQuery request,
        CancellationToken cancellationToken)
    {
        var dateRange = ReportQueryHelper.ValidateDateRange(request.FromDate, request.ToDate);
        if (!dateRange.IsSuccess)
            return Result.Failure<SalesReportDto>(dateRange.Error!, dateRange.ErrorCode);

        var tenantId = _tenantContext.TenantId;
        var (start, endExclusive) = dateRange.Data!;

        var query = ReportQueryHelper.ApplySalesOrderFilter(
            ReportQueryHelper.ApplyDateRange(
                ReportQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId),
                start,
                endExclusive));

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        var totalSales = await query.SumAsync(o => o.GrandTotal, cancellationToken);
        var orderCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new SalesReportItemDto(
                o.OrderNumber,
                o.CreatedAt,
                o.Branch.Name,
                o.Customer != null ? o.Customer.FullName : null,
                o.GrandTotal,
                o.Status))
            .ToListAsync(cancellationToken);

        return Result<SalesReportDto>.Success(new SalesReportDto(totalSales, orderCount, items));
    }
}
