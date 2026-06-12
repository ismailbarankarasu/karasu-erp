using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<PaginatedList<InvoiceListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetInvoicesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<InvoiceListDto>>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.TenantId == _tenantContext.TenantId && !i.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == request.CustomerId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListDto(
                i.Id,
                i.InvoiceNumber,
                i.OrderId,
                i.Order.OrderNumber,
                i.CustomerId,
                i.Customer.FullName,
                i.Type,
                i.Status,
                i.GrandTotal,
                i.IssuedAt,
                i.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<InvoiceListDto>>.Success(
            new PaginatedList<InvoiceListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
