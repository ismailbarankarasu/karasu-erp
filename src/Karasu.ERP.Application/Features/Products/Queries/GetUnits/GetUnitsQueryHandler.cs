using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Queries.GetUnits;

public class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, Result<IReadOnlyList<UnitDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetUnitsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<UnitDto>>> Handle(
        GetUnitsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.Units
            .AsNoTracking()
            .Where(u => u.TenantId == _tenantContext.TenantId && !u.IsDeleted)
            .OrderBy(u => u.Name)
            .Select(u => new UnitDto(u.Id, u.Name, u.Symbol))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UnitDto>>.Success(items);
    }
}
