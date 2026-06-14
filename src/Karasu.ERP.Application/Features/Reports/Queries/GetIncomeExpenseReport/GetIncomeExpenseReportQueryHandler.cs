using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Reports.Queries.GetIncomeExpenseReport;

public class GetIncomeExpenseReportQueryHandler
    : IRequestHandler<GetIncomeExpenseReportQuery, Result<IncomeExpenseReportDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetIncomeExpenseReportQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IncomeExpenseReportDto>> Handle(
        GetIncomeExpenseReportQuery request,
        CancellationToken cancellationToken)
    {
        var dateRange = ReportQueryHelper.ValidateDateRange(request.FromDate, request.ToDate);
        if (!dateRange.IsSuccess)
            return Result.Failure<IncomeExpenseReportDto>(dateRange.Error!, dateRange.ErrorCode);

        var tenantId = _tenantContext.TenantId;
        var (start, endExclusive) = dateRange.Data!;

        var totalIncome = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId &&
                        !i.IsDeleted &&
                        i.IncomeDate >= start &&
                        i.IncomeDate < endExclusive)
            .SumAsync(i => i.Amount, cancellationToken);

        var totalExpense = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        !e.IsDeleted &&
                        e.ExpenseDate >= start &&
                        e.ExpenseDate < endExclusive)
            .SumAsync(e => e.Amount, cancellationToken);

        var incomesByMonth = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId &&
                        !i.IsDeleted &&
                        i.IncomeDate >= start &&
                        i.IncomeDate < endExclusive)
            .GroupBy(i => new { i.IncomeDate.Year, i.IncomeDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var expensesByMonth = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        !e.IsDeleted &&
                        e.ExpenseDate >= start &&
                        e.ExpenseDate < endExclusive)
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var monthlyBreakdown = new List<IncomeExpenseMonthDto>();
        var monthCursor = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = new DateTime(endExclusive.AddDays(-1).Year, endExclusive.AddDays(-1).Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (monthCursor <= lastMonth)
        {
            var income = incomesByMonth
                .FirstOrDefault(x => x.Year == monthCursor.Year && x.Month == monthCursor.Month)?.Total ?? 0;
            var expense = expensesByMonth
                .FirstOrDefault(x => x.Year == monthCursor.Year && x.Month == monthCursor.Month)?.Total ?? 0;

            monthlyBreakdown.Add(new IncomeExpenseMonthDto(
                monthCursor.ToString("yyyy-MM"),
                monthCursor.Year,
                monthCursor.Month,
                income,
                expense,
                income - expense));

            monthCursor = monthCursor.AddMonths(1);
        }

        return Result<IncomeExpenseReportDto>.Success(new IncomeExpenseReportDto(
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            monthlyBreakdown));
    }
}
