using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomers;

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, Result<PaginatedList<CustomerListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICacheService _cacheService;

    public GetCustomersQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICacheService cacheService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _cacheService = cacheService;
    }

    public async Task<Result<PaginatedList<CustomerListDto>>> Handle(
        GetCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey =
            $"{_tenantContext.TenantId}:customer:list:{request.Page}:{request.PageSize}:{request.SearchTerm}:{request.Type}:{request.Status}";

        var cached = await _cacheService.GetAsync<PaginatedList<CustomerListDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result<PaginatedList<CustomerListDto>>.Success(cached);

        var query = _context.Customers
            .AsNoTracking()
            .Where(c => c.TenantId == _tenantContext.TenantId && !c.IsDeleted);

        if (request.Type.HasValue)
            query = query.Where(c => c.Type == request.Type.Value);

        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.FullName.Contains(term) ||
                (c.CompanyName != null && c.CompanyName.Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.Email != null && c.Email.Contains(term)) ||
                (c.TaxNumber != null && c.TaxNumber.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerListDto(
                c.Id,
                c.Type,
                c.FullName,
                c.CompanyName,
                c.Phone,
                c.Email,
                c.City,
                c.Balance,
                c.CreditLimit,
                c.Status,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = new PaginatedList<CustomerListDto>(items, totalCount, request.Page, request.PageSize);
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result<PaginatedList<CustomerListDto>>.Success(result);
    }
}
