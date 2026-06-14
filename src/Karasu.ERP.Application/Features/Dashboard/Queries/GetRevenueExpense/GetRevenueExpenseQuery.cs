using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetRevenueExpense;

public record GetRevenueExpenseQuery() : IRequest<Result<List<RevenueExpenseItemDto>>>;

public record RevenueExpenseItemDto(
    string MonthLabel,
    int Year,
    int Month,
    decimal Income,
    decimal Expense);
