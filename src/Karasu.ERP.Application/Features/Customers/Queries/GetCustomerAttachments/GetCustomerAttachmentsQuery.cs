using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerAttachments;

public record GetCustomerAttachmentsQuery(Guid CustomerId) : IRequest<Result<List<CustomerAttachmentDto>>>;

public record CustomerAttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime CreatedAt);

public class GetCustomerAttachmentsQueryHandler : IRequestHandler<GetCustomerAttachmentsQuery, Result<List<CustomerAttachmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCustomerAttachmentsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<CustomerAttachmentDto>>> Handle(
        GetCustomerAttachmentsQuery request,
        CancellationToken cancellationToken)
    {
        var customerExists = await _context.Customers.AnyAsync(
            c => c.Id == request.CustomerId &&
                 c.TenantId == _tenantContext.TenantId &&
                 !c.IsDeleted,
            cancellationToken);

        if (!customerExists)
            return Result<List<CustomerAttachmentDto>>.Failure("Müşteri bulunamadı.", "CUSTOMER_NOT_FOUND");

        var attachments = await _context.CustomerAttachments
            .AsNoTracking()
            .Where(a => a.CustomerId == request.CustomerId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new CustomerAttachmentDto(
                a.Id,
                a.FileName,
                a.ContentType,
                a.FileSize,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<List<CustomerAttachmentDto>>.Success(attachments);
    }
}
