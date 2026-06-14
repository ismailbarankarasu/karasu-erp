using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetRevenueExpense;

public class GetRevenueExpenseQueryHandler : IRequestHandler<GetRevenueExpenseQuery, Result<List<RevenueExpenseItemDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetRevenueExpenseQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<List<RevenueExpenseItemDto>>> Handle(
        GetRevenueExpenseQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;
        var cacheKey = $"{tenantId}:dashboard:revenue-expense:{now:yyyy-MM-dd}";

        var data = await _cacheService.GetOrSetAsync(
            cacheKey,
            () => LoadRevenueExpenseAsync(tenantId, now, cancellationToken),
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return Result<List<RevenueExpenseItemDto>>.Success(data);
    }

    private async Task<List<RevenueExpenseItemDto>> LoadRevenueExpenseAsync(
        Guid tenantId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var monthStart = DashboardQueryHelper.GetMonthStartUtc(now);
        var rangeStart = monthStart.AddMonths(-5);

        var incomes = await _context.Incomes
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId &&
                        !i.IsDeleted &&
                        i.IncomeDate >= rangeStart &&
                        i.IncomeDate < monthStart.AddMonths(1))
            .GroupBy(i => new { i.IncomeDate.Year, i.IncomeDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var expenses = await _context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        !e.IsDeleted &&
                        e.ExpenseDate >= rangeStart &&
                        e.ExpenseDate < monthStart.AddMonths(1))
            .GroupBy(e => new { e.ExpenseDate.Year, e.ExpenseDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var result = new List<RevenueExpenseItemDto>();

        for (var i = 0; i < 6; i++)
        {
            var monthDate = monthStart.AddMonths(-5 + i);
            var year = monthDate.Year;
            var month = monthDate.Month;
            var income = incomes.FirstOrDefault(x => x.Year == year && x.Month == month)?.Total ?? 0;
            var expense = expenses.FirstOrDefault(x => x.Year == year && x.Month == month)?.Total ?? 0;

            result.Add(new RevenueExpenseItemDto(
                monthDate.ToString("yyyy-MM"),
                year,
                month,
                income,
                expense));
        }

        return result;
    }
}
