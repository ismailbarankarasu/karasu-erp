using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetInvoiceByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<InvoiceDetailDto>> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == request.Id && i.TenantId == _tenantContext.TenantId && !i.IsDeleted)
            .Select(i => new InvoiceDetailDto(
                i.Id,
                i.InvoiceNumber,
                i.OrderId,
                i.Order.OrderNumber,
                i.CustomerId,
                i.Customer.FullName,
                i.Type,
                i.Status,
                i.SubTotal,
                i.TaxTotal,
                i.GrandTotal,
                i.IssuedAt,
                i.CreatedAt,
                i.Lines.Select(l => new InvoiceLineDto(
                    l.Id,
                    l.ProductVariantId,
                    l.Description,
                    l.Quantity,
                    l.UnitPrice,
                    l.TaxRate,
                    l.LineTotal)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return Result<InvoiceDetailDto>.Failure("Fatura bulunamadı.", "INVOICE_NOT_FOUND");

        return Result<InvoiceDetailDto>.Success(invoice);
    }
}
