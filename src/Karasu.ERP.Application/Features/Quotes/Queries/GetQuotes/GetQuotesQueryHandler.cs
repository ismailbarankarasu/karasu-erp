using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Quotes.Queries.GetQuotes;

public class GetQuotesQueryHandler : IRequestHandler<GetQuotesQuery, Result<PaginatedList<QuoteListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetQuotesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<QuoteListDto>>> Handle(
        GetQuotesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Quotes
            .AsNoTracking()
            .Where(q => q.TenantId == _tenantContext.TenantId && !q.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(q => q.Status == request.Status.Value);

        if (request.CustomerId.HasValue)
            query = query.Where(q => q.CustomerId == request.CustomerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(q => new QuoteListDto(
                q.Id,
                q.QuoteNumber,
                q.Status,
                q.CustomerId,
                q.Customer != null ? q.Customer.FullName : null,
                q.GrandTotal,
                q.ValidUntil,
                q.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<QuoteListDto>>.Success(
            new PaginatedList<QuoteListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
