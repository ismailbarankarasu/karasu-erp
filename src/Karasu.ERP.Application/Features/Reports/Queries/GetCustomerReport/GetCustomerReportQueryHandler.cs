using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetCustomerReport;

public class GetCustomerReportQueryHandler : IRequestHandler<GetCustomerReportQuery, Result<CustomerReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCustomerReportQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CustomerReportDto>> Handle(
        GetCustomerReportQuery request,
        CancellationToken cancellationToken)
    {
        var dateRange = ReportQueryHelper.ValidateDateRange(request.FromDate, request.ToDate);
        if (!dateRange.IsSuccess)
            return Result.Failure<CustomerReportDto>(dateRange.Error!, dateRange.ErrorCode);

        var tenantId = _tenantContext.TenantId;
        var (start, endExclusive) = dateRange.Data!;

        var items = await ReportQueryHelper.ApplySalesOrderFilter(
                ReportQueryHelper.ApplyDateRange(
                    ReportQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId),
                    start,
                    endExclusive))
            .Where(o => o.CustomerId != null)
            .GroupBy(o => o.Customer!.FullName)
            .Select(g => new CustomerReportItemDto(
                g.Key,
                g.Count(),
                g.Sum(x => x.GrandTotal)))
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync(cancellationToken);

        return Result<CustomerReportDto>.Success(new CustomerReportDto(items));
    }
}
