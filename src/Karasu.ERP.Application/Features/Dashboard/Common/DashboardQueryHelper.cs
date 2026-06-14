using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Application.Features.Dashboard.Common;

public static class DashboardQueryHelper
{
    public static readonly OrderStatus[] SalesOrderStatuses =
    {
        OrderStatus.Confirmed,
        OrderStatus.Preparing,
        OrderStatus.Shipping,
        OrderStatus.Delivered
    };

    public static readonly OrderStatus[] PendingOrderStatuses =
    {
        OrderStatus.Draft,
        OrderStatus.Pending,
        OrderStatus.Confirmed,
        OrderStatus.Preparing,
        OrderStatus.Shipping
    };

    public static IQueryable<Order> ForTenantOrders(IQueryable<Order> query, Guid tenantId) =>
        query.Where(o => o.TenantId == tenantId && !o.IsDeleted);

    public static IQueryable<Order> ApplySalesOrderFilter(IQueryable<Order> query) =>
        query.Where(o => SalesOrderStatuses.Contains(o.Status));

    public static IQueryable<Order> ApplyPendingOrderFilter(IQueryable<Order> query) =>
        query.Where(o => PendingOrderStatuses.Contains(o.Status));

    public static DateTime GetTodayStartUtc(DateTime utcNow) =>
        new(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime GetMonthStartUtc(DateTime utcNow) =>
        new(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    public static (DateTime Start, DateTime End) GetTodayRange(DateTime utcNow)
    {
        var start = GetTodayStartUtc(utcNow);
        return (start, start.AddDays(1));
    }

    public static (DateTime Start, DateTime End) GetMonthRange(DateTime utcNow)
    {
        var start = GetMonthStartUtc(utcNow);
        return (start, start.AddMonths(1));
    }

    public static (DateTime Start, DateTime End) ResolveSalesTrendRange(
        SalesTrendPeriod period,
        DateTime? fromDate,
        DateTime? toDate,
        DateTime utcNow)
    {
        if (fromDate.HasValue && toDate.HasValue)
            return (fromDate.Value.Date, toDate.Value.Date.AddDays(1));

        return period switch
        {
            SalesTrendPeriod.Daily => (utcNow.Date.AddDays(-29), utcNow.Date.AddDays(1)),
            SalesTrendPeriod.Weekly => (utcNow.Date.AddDays(-7 * 11), utcNow.Date.AddDays(1)),
            SalesTrendPeriod.Monthly => (utcNow.Date.AddMonths(-11), utcNow.Date.AddDays(1)),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }
}

public enum SalesTrendPeriod
{
    Daily,
    Weekly,
    Monthly
}
