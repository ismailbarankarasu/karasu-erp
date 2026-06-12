using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Quotes.Queries.GetQuoteById;

public class GetQuoteByIdQueryHandler : IRequestHandler<GetQuoteByIdQuery, Result<QuoteDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetQuoteByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<QuoteDetailDto>> Handle(
        GetQuoteByIdQuery request,
        CancellationToken cancellationToken)
    {
        var quote = await _context.Quotes
            .AsNoTracking()
            .Where(q => q.Id == request.Id && q.TenantId == _tenantContext.TenantId && !q.IsDeleted)
            .Select(q => new QuoteDetailDto(
                q.Id,
                q.QuoteNumber,
                q.Status,
                q.BranchId,
                q.CustomerId,
                q.Customer != null ? q.Customer.FullName : null,
                q.SubTotal,
                q.TaxTotal,
                q.DiscountTotal,
                q.GrandTotal,
                q.ValidUntil,
                q.Notes,
                q.ConvertedOrderId,
                q.CreatedAt,
                q.Lines.Select(l => new QuoteLineDto(
                    l.Id,
                    l.ProductVariantId,
                    l.ProductVariant.Product.Name,
                    l.ProductVariant.Sku,
                    l.Quantity,
                    l.UnitPrice,
                    l.TaxRate,
                    l.Discount,
                    l.LineTotal)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (quote is null)
            return Result<QuoteDetailDto>.Failure("Teklif bulunamadı.", "QUOTE_NOT_FOUND");

        return Result<QuoteDetailDto>.Success(quote);
    }
}
