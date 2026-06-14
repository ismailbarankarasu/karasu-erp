using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Queries.GetSuppliers;

public record GetSuppliersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IRequest<Result<PaginatedList<SupplierListDto>>>;

public record SupplierListDto(
    Guid Id,
    string Name,
    string? TaxNumber,
    string? ContactPerson,
    string? Phone,
    decimal Balance,
    decimal Rating,
    bool IsActive);

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, Result<PaginatedList<SupplierListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetSuppliersQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<SupplierListDto>>> Handle(
        GetSuppliersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Suppliers
            .AsNoTracking()
            .Where(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                (s.TaxNumber != null && s.TaxNumber.Contains(term)) ||
                (s.ContactPerson != null && s.ContactPerson.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new SupplierListDto(
                s.Id, s.Name, s.TaxNumber, s.ContactPerson, s.Phone,
                s.Balance, s.Rating, s.IsActive))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<SupplierListDto>>.Success(
            new PaginatedList<SupplierListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
