using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerNotes;

public class GetCustomerNotesQueryHandler : IRequestHandler<GetCustomerNotesQuery, Result<IReadOnlyList<CustomerNoteDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCustomerNotesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<CustomerNoteDto>>> Handle(
        GetCustomerNotesQuery request,
        CancellationToken cancellationToken)
    {
        var customerExists = await _context.Customers
            .AnyAsync(
                c => c.Id == request.CustomerId && c.TenantId == _tenantContext.TenantId && !c.IsDeleted,
                cancellationToken);

        if (!customerExists)
            return Result<IReadOnlyList<CustomerNoteDto>>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        var notes = await _context.CustomerNotes
            .AsNoTracking()
            .Where(n => n.CustomerId == request.CustomerId && n.TenantId == _tenantContext.TenantId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new CustomerNoteDto(n.Id, n.Content, n.CreatedByUserId, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CustomerNoteDto>>.Success(notes);
    }
}
