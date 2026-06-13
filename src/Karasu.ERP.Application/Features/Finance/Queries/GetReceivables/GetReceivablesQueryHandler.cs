using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetReceivables;

public class GetReceivablesQueryHandler : IRequestHandler<GetReceivablesQuery, Result<List<ReceivableDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetReceivablesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<ReceivableDto>>> Handle(
        GetReceivablesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Receivables
            .AsNoTracking()
            .Where(r => r.TenantId == _tenantContext.TenantId && !r.IsDeleted);

        if (request.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == request.CustomerId.Value);

        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);

        var items = await query
            .OrderBy(r => r.DueDate)
            .ThenByDescending(r => r.CreatedAt)
            .Select(r => new ReceivableDto(
                r.Id,
                r.CustomerId,
                r.Customer.FullName,
                r.InvoiceId,
                r.OrderId,
                r.Amount,
                r.PaidAmount,
                r.Amount - r.PaidAmount,
                r.DueDate,
                r.Status,
                r.Description))
            .ToListAsync(cancellationToken);

        return Result<List<ReceivableDto>>.Success(items);
    }
}
