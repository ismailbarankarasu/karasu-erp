using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetPayables;

public class GetPayablesQueryHandler : IRequestHandler<GetPayablesQuery, Result<List<PayableDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetPayablesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<PayableDto>>> Handle(
        GetPayablesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Payables
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        var items = await query
            .OrderBy(p => p.DueDate)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new PayableDto(
                p.Id,
                p.CreditorName,
                p.SupplierId,
                p.Amount,
                p.PaidAmount,
                p.Amount - p.PaidAmount,
                p.DueDate,
                p.Status,
                p.Description))
            .ToListAsync(cancellationToken);

        return Result<List<PayableDto>>.Success(items);
    }
}
