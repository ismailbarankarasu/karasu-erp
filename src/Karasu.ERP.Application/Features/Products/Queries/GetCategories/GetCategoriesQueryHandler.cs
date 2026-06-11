using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Queries.GetCategories;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCategoriesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _context.Categories
            .AsNoTracking()
            .Where(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.ParentId, c.SortOrder))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CategoryDto>>.Success(items);
    }
}
