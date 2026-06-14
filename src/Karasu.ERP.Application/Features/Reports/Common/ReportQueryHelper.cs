using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;

namespace Karasu.ERP.Application.Features.Reports.Common;

public static class ReportQueryHelper
{
    public static readonly OrderStatus[] SalesOrderStatuses =
    {
        OrderStatus.Confirmed,
        OrderStatus.Preparing,
        OrderStatus.Shipping,
        OrderStatus.Delivered
    };

    public static IQueryable<Order> ForTenantOrders(IQueryable<Order> query, Guid tenantId) =>
        query.Where(o => o.TenantId == tenantId && !o.IsDeleted);

    public static IQueryable<Order> ApplySalesOrderFilter(IQueryable<Order> query) =>
        query.Where(o => SalesOrderStatuses.Contains(o.Status));

    public static Result<(DateTime Start, DateTime EndExclusive)> ValidateDateRange(DateTime fromDate, DateTime toDate)
    {
        if (toDate.Date < fromDate.Date)
            return Result.Failure<(DateTime Start, DateTime EndExclusive)>(
                "Bitiş tarihi başlangıç tarihinden önce olamaz.",
                "INVALID_DATE_RANGE");

        var start = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        var endExclusive = DateTime.SpecifyKind(toDate.Date.AddDays(1), DateTimeKind.Utc);
        return Result.Success((start, endExclusive));
    }

    public static IQueryable<Order> ApplyDateRange(IQueryable<Order> query, DateTime start, DateTime endExclusive) =>
        query.Where(o => o.CreatedAt >= start && o.CreatedAt < endExclusive);
}
