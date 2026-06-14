using System.Globalization;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetSalesTrend;

public class GetSalesTrendQueryHandler : IRequestHandler<GetSalesTrendQuery, Result<List<SalesTrendItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetSalesTrendQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<SalesTrendItemDto>>> Handle(
        GetSalesTrendQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var fromKey = request.FromDate?.ToString("yyyy-MM-dd") ?? "all";
        var toKey = request.ToDate?.ToString("yyyy-MM-dd") ?? "all";
        var branchKey = request.BranchId?.ToString() ?? "all";
        var cacheKey =
            $"{tenantId}:dashboard:sales-trend:{request.Period}:{fromKey}:{toKey}:{branchKey}";

        var trend = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadTrendAsync(tenantId, request, cancellationToken),
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return Result<List<SalesTrendItemDto>>.Success(trend);
    }

    private async Task<List<SalesTrendItemDto>> LoadTrendAsync(
        Guid tenantId,
        GetSalesTrendQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var (rangeStart, rangeEnd) = DashboardQueryHelper.ResolveSalesTrendRange(
            request.Period,
            request.FromDate,
            request.ToDate,
            now);

        var query = DashboardQueryHelper.ApplySalesOrderFilter(
            DashboardQueryHelper.ForTenantOrders(_context.Orders.AsNoTracking(), tenantId))
            .Where(o => o.CreatedAt >= rangeStart && o.CreatedAt < rangeEnd);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        var orders = await query
            .Select(o => new OrderTrendRow(o.CreatedAt, o.GrandTotal))
            .ToListAsync(cancellationToken);

        return request.Period switch
        {
            SalesTrendPeriod.Daily => BuildDailyTrend(rangeStart, rangeEnd, orders),
            SalesTrendPeriod.Weekly => BuildWeeklyTrend(rangeStart, rangeEnd, orders),
            SalesTrendPeriod.Monthly => BuildMonthlyTrend(rangeStart, rangeEnd, orders),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Period))
        };
    }

    private static List<SalesTrendItemDto> BuildDailyTrend(
        DateTime rangeStart,
        DateTime rangeEnd,
        IReadOnlyList<OrderTrendRow> orders)
    {
        var salesByDay = orders
            .GroupBy(o => o.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (Total: g.Sum(x => x.GrandTotal), Count: g.Count()));

        var result = new List<SalesTrendItemDto>();
        for (var day = rangeStart.Date; day < rangeEnd.Date; day = day.AddDays(1))
        {
            salesByDay.TryGetValue(day, out var sales);
            result.Add(new SalesTrendItemDto(
                day.ToString("yyyy-MM-dd"),
                day,
                sales.Total,
                sales.Count));
        }

        return result;
    }

    private static List<SalesTrendItemDto> BuildWeeklyTrend(
        DateTime rangeStart,
        DateTime rangeEnd,
        IReadOnlyList<OrderTrendRow> orders)
    {
        var salesByWeek = orders
            .GroupBy(o => GetWeekStart(o.CreatedAt.Date))
            .ToDictionary(g => g.Key, g => (Total: g.Sum(x => x.GrandTotal), Count: g.Count()));

        var result = new List<SalesTrendItemDto>();
        for (var weekStart = GetWeekStart(rangeStart.Date); weekStart < rangeEnd.Date; weekStart = weekStart.AddDays(7))
        {
            salesByWeek.TryGetValue(weekStart, out var sales);
            result.Add(new SalesTrendItemDto(
                $"W{ISOWeek.GetWeekOfYear(weekStart):D2}-{weekStart:yyyy}",
                weekStart,
                sales.Total,
                sales.Count));
        }

        return result;
    }

    private static List<SalesTrendItemDto> BuildMonthlyTrend(
        DateTime rangeStart,
        DateTime rangeEnd,
        IReadOnlyList<OrderTrendRow> orders)
    {
        var salesByMonth = orders
            .GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1))
            .ToDictionary(g => g.Key, g => (Total: g.Sum(x => x.GrandTotal), Count: g.Count()));

        var result = new List<SalesTrendItemDto>();
        for (var month = new DateTime(rangeStart.Year, rangeStart.Month, 1);
             month < rangeEnd.Date;
             month = month.AddMonths(1))
        {
            salesByMonth.TryGetValue(month, out var sales);
            result.Add(new SalesTrendItemDto(
                month.ToString("yyyy-MM"),
                month,
                sales.Total,
                sales.Count));
        }

        return result;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff);
    }

    private sealed record OrderTrendRow(DateTime CreatedAt, decimal GrandTotal);
}
