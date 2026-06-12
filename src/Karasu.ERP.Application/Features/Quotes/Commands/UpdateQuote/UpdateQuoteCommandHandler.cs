using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Quotes.Commands.UpdateQuote;

public class UpdateQuoteCommandHandler : IRequestHandler<UpdateQuoteCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateQuoteCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateQuoteCommand request, CancellationToken cancellationToken)
    {
        var quote = await _context.Quotes
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(
                q => q.Id == request.Id && q.TenantId == _tenantContext.TenantId && !q.IsDeleted,
                cancellationToken);

        if (quote is null)
            return Result.Failure("Teklif bulunamadı.", "QUOTE_NOT_FOUND");

        if (quote.Status != QuoteStatus.Draft)
            return Result.Failure("Sadece taslak teklifler güncellenebilir.", "QUOTE_INVALID_STATUS");

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variantCount = await _context.ProductVariants.CountAsync(
            v => variantIds.Contains(v.Id) && v.TenantId == _tenantContext.TenantId && !v.IsDeleted,
            cancellationToken);
        if (variantCount != variantIds.Count)
            return Result.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        try
        {
            quote.UpdateDraft(
                request.BranchId,
                request.CustomerId,
                request.Notes,
                request.ValidUntil,
                request.Lines.Select(l => (l.ProductVariantId, l.Quantity, l.UnitPrice, l.TaxRate, l.Discount)));
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "QUOTE_INVALID_STATUS");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
