using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetProfitLossReport;

public class GetProfitLossReportQueryHandler : IRequestHandler<GetProfitLossReportQuery, Result<ProfitLossReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetProfitLossReportQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProfitLossReportDto>> Handle(
        GetProfitLossReportQuery request,
        CancellationToken cancellationToken)
    {
        var dateRange = ReportQueryHelper.ValidateDateRange(request.FromDate, request.ToDate);
        if (!dateRange.IsSuccess)
            return Result.Failure<ProfitLossReportDto>(dateRange.Error!, dateRange.ErrorCode);

        var tenantId = _tenantContext.TenantId;
        var (start, endExclusive) = dateRange.Data!;

        var salesOrders = ReportQueryHelper.ApplySalesOrderFilter(
            ReportQueryHelper.ApplyDateRange(
                ReportQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId),
                start,
                endExclusive));

        var revenue = await salesOrders.SumAsync(o => o.GrandTotal, cancellationToken);

        var cogs = await _context.OrderLines
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId &&
                        !l.IsDeleted &&
                        !l.Order.IsDeleted &&
                        ReportQueryHelper.SalesOrderStatuses.Contains(l.Order.Status) &&
                        l.Order.CreatedAt >= start &&
                        l.Order.CreatedAt < endExclusive)
            .SumAsync(l => l.Quantity * l.ProductVariant.Product.PurchasePrice, cancellationToken);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        !e.IsDeleted &&
                        e.ExpenseDate >= start &&
                        e.ExpenseDate < endExclusive)
            .SumAsync(e => e.Amount, cancellationToken);

        var profit = revenue - cogs - expenses;

        return Result<ProfitLossReportDto>.Success(new ProfitLossReportDto(revenue, cogs, expenses, profit));
    }
}
