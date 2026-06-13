using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetExpenses;

public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, Result<PaginatedList<ExpenseDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetExpensesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<ExpenseDto>>> Handle(
        GetExpensesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Expenses
            .AsNoTracking()
            .Where(e => e.TenantId == _tenantContext.TenantId && !e.IsDeleted);

        if (request.FromDate.HasValue)
            query = query.Where(e => e.ExpenseDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(e => e.ExpenseDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new ExpenseDto(
                e.Id,
                e.CategoryId,
                e.Category != null ? e.Category.Name : null,
                e.Amount,
                e.Description,
                e.ExpenseDate,
                e.PaymentMethod,
                e.CashRegisterId,
                e.BankAccountId,
                e.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<ExpenseDto>>.Success(
            new PaginatedList<ExpenseDto>(items, totalCount, request.Page, request.PageSize));
    }
}
