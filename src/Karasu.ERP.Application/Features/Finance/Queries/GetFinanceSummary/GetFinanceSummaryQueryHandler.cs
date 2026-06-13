using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetFinanceSummary;

public class GetFinanceSummaryQueryHandler : IRequestHandler<GetFinanceSummaryQuery, Result<FinanceSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetFinanceSummaryQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<FinanceSummaryDto>> Handle(
        GetFinanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var totalCashBalance = await _context.CashRegisters
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive && !c.IsDeleted)
            .SumAsync(c => c.CurrentBalance, cancellationToken);

        var totalBankBalance = await _context.BankAccounts
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.IsActive && !b.IsDeleted)
            .SumAsync(b => b.CurrentBalance, cancellationToken);

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

        var openPayableStatuses = new[]
        {
            PayableStatus.Open,
            PayableStatus.PartiallyPaid,
            PayableStatus.Overdue
        };

        var totalPayables = await _context.Payables
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId &&
                        openPayableStatuses.Contains(p.Status) &&
                        !p.IsDeleted)
            .SumAsync(p => p.Amount - p.PaidAmount, cancellationToken);

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

        return Result<FinanceSummaryDto>.Success(new FinanceSummaryDto(
            totalCashBalance,
            totalBankBalance,
            totalReceivables,
            totalPayables,
            monthIncome,
            monthExpense));
    }
}
