using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery() : IRequest<Result<DashboardSummaryDto>>;

public record DashboardSummaryDto(
    decimal TodaySales,
    decimal MonthSales,
    int TodayOrderCount,
    int MonthOrderCount,
    int PendingOrdersCount,
    int ActiveCustomersCount,
    int CriticalStockCount,
    decimal TotalReceivables,
    decimal MonthIncome,
    decimal MonthExpense);
