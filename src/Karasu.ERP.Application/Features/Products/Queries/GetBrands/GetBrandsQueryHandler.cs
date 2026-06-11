using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Queries.GetBrands;

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, Result<IReadOnlyList<BrandDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetBrandsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<BrandDto>>> Handle(
        GetBrandsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.Brands
            .AsNoTracking()
            .Where(b => b.TenantId == _tenantContext.TenantId && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BrandDto>>.Success(items);
    }
}
