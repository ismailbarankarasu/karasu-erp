using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Quotes.Commands.CreateQuote;

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQuoteCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateQuoteCommand request, CancellationToken cancellationToken)
    {
        if (request.BranchId.HasValue)
        {
            var branchExists = await _context.Branches.AnyAsync(
                b => b.Id == request.BranchId.Value && b.TenantId == _tenantContext.TenantId && !b.IsDeleted,
                cancellationToken);
            if (!branchExists)
                return Result<Guid>.Failure("Geçersiz şube.", "BRANCH_NOT_FOUND");
        }

        if (request.CustomerId.HasValue)
        {
            var customerExists = await _context.Customers.AnyAsync(
                c => c.Id == request.CustomerId.Value && c.TenantId == _tenantContext.TenantId && !c.IsDeleted,
                cancellationToken);
            if (!customerExists)
                return Result<Guid>.Failure("Geçersiz müşteri.", "CUSTOMER_NOT_FOUND");
        }

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var variantCount = await _context.ProductVariants.CountAsync(
            v => variantIds.Contains(v.Id) && v.TenantId == _tenantContext.TenantId && !v.IsDeleted,
            cancellationToken);
        if (variantCount != variantIds.Count)
            return Result<Guid>.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        var quoteNumber = await GenerateQuoteNumberAsync(cancellationToken);
        var quote = Quote.Create(
            _tenantContext.TenantId,
            request.BranchId,
            request.CustomerId,
            quoteNumber,
            request.ValidUntil);

        quote.Notes = request.Notes;

        foreach (var line in request.Lines)
            quote.AddLine(line.ProductVariantId, line.Quantity, line.UnitPrice, line.TaxRate, line.Discount);

        await _context.Quotes.AddAsync(quote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(quote.Id);
    }

    private async Task<string> GenerateQuoteNumberAsync(CancellationToken ct)
    {
        var count = await _context.Quotes.CountAsync(ct);
        return $"QT-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D5}";
    }
}
