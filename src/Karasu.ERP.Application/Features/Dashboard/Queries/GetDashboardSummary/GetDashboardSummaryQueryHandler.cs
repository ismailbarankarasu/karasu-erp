using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetDashboardSummaryQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<DashboardSummaryDto>> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;
        var cacheKey = $"{tenantId}:dashboard:summary:{now:yyyy-MM-dd}";

        var summary = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadSummaryAsync(tenantId, now, cancellationToken),
            TimeSpan.FromMinutes(2),
            cancellationToken);

        return Result<DashboardSummaryDto>.Success(summary);
    }

    private async Task<DashboardSummaryDto> LoadSummaryAsync(
        Guid tenantId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var (todayStart, todayEnd) = DashboardQueryHelper.GetTodayRange(now);
        var (monthStart, monthEnd) = DashboardQueryHelper.GetMonthRange(now);

        var salesOrders = DashboardQueryHelper.ApplySalesOrderFilter(
            DashboardQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId));

        var todaySales = await salesOrders
            .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd)
            .SumAsync(o => o.GrandTotal, cancellationToken);

        var monthSales = await salesOrders
            .Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd)
            .SumAsync(o => o.GrandTotal, cancellationToken);

        var todayOrderCount = await salesOrders
            .CountAsync(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd, cancellationToken);

        var monthOrderCount = await salesOrders
            .CountAsync(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd, cancellationToken);

        var pendingOrdersCount = await DashboardQueryHelper.ApplyPendingOrderFilter(
                DashboardQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId))
            .CountAsync(cancellationToken);

        var activeCustomersCount = await _context.Customers
            .AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId &&
                             c.Status == CustomerStatus.Active &&
                             !c.IsDeleted,
                cancellationToken);

        var criticalStockCount = await _context.StockItems
            .AsNoTracking()
            .CountAsync(s => s.TenantId == tenantId &&
                             !s.IsDeleted &&
                             s.MinStock > 0 &&
                             s.Quantity <= s.MinStock,
                cancellationToken);

        var openReceivableStatuses = new[]
        {
            ReceivableStatus.Open,
            ReceivableStatus.PartiallyPaid,
            ReceivableStatus.Overdue
        };

        var totalReceivables = await _context.Receivables
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId &&
                        openReceivableStatuses.Contains(r.Status) &&
                        !r.IsDeleted)
            .SumAsync(r => r.Amount - r.PaidAmount, cancellationToken);

        var monthIncome = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId &&
                        i.IncomeDate >= monthStart &&
                        i.IncomeDate < monthEnd &&
                        !i.IsDeleted)
            .SumAsync(i => i.Amount, cancellationToken);

        var monthExpense = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        e.ExpenseDate >= monthStart &&
                        e.ExpenseDate < monthEnd &&
                        !e.IsDeleted)
            .SumAsync(e => e.Amount, cancellationToken);

        return new DashboardSummaryDto(
            todaySales,
            monthSales,
            todayOrderCount,
            monthOrderCount,
            pendingOrdersCount,
            activeCustomersCount,
            criticalStockCount,
            totalReceivables,
            monthIncome,
            monthExpense);
    }
}
