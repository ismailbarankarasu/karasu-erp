using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetIncomes;

public class GetIncomesQueryHandler : IRequestHandler<GetIncomesQuery, Result<PaginatedList<IncomeDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetIncomesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<IncomeDto>>> Handle(
        GetIncomesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Incomes
            .AsNoTracking()
            .Where(i => i.TenantId == _tenantContext.TenantId && !i.IsDeleted);

        if (request.FromDate.HasValue)
            query = query.Where(i => i.IncomeDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(i => i.IncomeDate <= request.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.IncomeDate)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new IncomeDto(
                i.Id,
                i.CategoryId,
                i.Category != null ? i.Category.Name : null,
                i.Amount,
                i.Description,
                i.IncomeDate,
                i.Source,
                i.CashRegisterId,
                i.BankAccountId,
                i.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<IncomeDto>>.Success(
            new PaginatedList<IncomeDto>(items, totalCount, request.Page, request.PageSize));
    }
}
