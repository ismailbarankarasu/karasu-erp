using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Products.Queries.GetProducts;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PaginatedList<ProductListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetProductsQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<PaginatedList<ProductListDto>>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey =
            $"{_tenantContext.TenantId}:product:list:{request.Page}:{request.PageSize}:{request.SearchTerm}:{request.CategoryId}:{request.Status}";

        var cached = await _cacheService.GetAsync<PaginatedList<ProductListDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result<PaginatedList<ProductListDto>>.Success(cached);

        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.Sku.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListDto(
                p.Id,
                p.Sku,
                p.Barcode,
                p.Name,
                p.Category != null ? p.Category.Name : null,
                p.Brand != null ? p.Brand.Name : null,
                p.Unit.Symbol,
                p.SalePrice,
                p.Status,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = new PaginatedList<ProductListDto>(items, totalCount, request.Page, request.PageSize);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result<PaginatedList<ProductListDto>>.Success(result);
    }
}
