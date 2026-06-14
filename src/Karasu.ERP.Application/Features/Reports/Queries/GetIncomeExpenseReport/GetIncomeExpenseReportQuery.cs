using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetIncomeExpenseReport;

public record GetIncomeExpenseReportQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<Result<IncomeExpenseReportDto>>;

public record IncomeExpenseReportDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Net,
    IReadOnlyList<IncomeExpenseMonthDto> MonthlyBreakdown);

public record IncomeExpenseMonthDto(
    string Month,
    int Year,
    int MonthNumber,
    decimal Income,
    decimal Expense,
    decimal Net);
