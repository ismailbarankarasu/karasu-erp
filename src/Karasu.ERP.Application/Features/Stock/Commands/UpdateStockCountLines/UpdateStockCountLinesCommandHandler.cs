using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Stock.Commands.UpdateStockCountLines;

public class UpdateStockCountLinesCommandHandler : IRequestHandler<UpdateStockCountLinesCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStockCountLinesCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateStockCountLinesCommand request,
        CancellationToken cancellationToken)
    {
        var count = await _context.StockCounts
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(
                c => c.Id == request.CountId &&
                     c.TenantId == _tenantContext.TenantId &&
                     !c.IsDeleted,
                cancellationToken);

        if (count is null)
            return Result.Failure("Sayım bulunamadı.", "COUNT_NOT_FOUND");

        if (count.Status != StockCountStatus.InProgress)
            return Result.Failure("Sadece devam eden sayımlar güncellenebilir.", "COUNT_INVALID_STATUS");

        foreach (var update in request.Lines)
        {
            StockCountLine? line = null;

            if (update.LineId.HasValue)
            {
                line = count.Lines.FirstOrDefault(l => l.Id == update.LineId.Value);
                if (line is null)
                    return Result.Failure("Sayım satırı bulunamadı.", "COUNT_LINE_NOT_FOUND");
            }
            else if (update.ProductVariantId.HasValue)
            {
                line = count.Lines.FirstOrDefault(l => l.ProductVariantId == update.ProductVariantId.Value);
                if (line is null)
                {
                    var variantExists = await _context.ProductVariants.AnyAsync(
                        v => v.Id == update.ProductVariantId.Value &&
                             v.TenantId == _tenantContext.TenantId &&
                             !v.IsDeleted,
                        cancellationToken);

                    if (!variantExists)
                        return Result.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

                    var stockItem = await _context.StockItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            s => s.WarehouseId == count.WarehouseId &&
                                 s.ProductVariantId == update.ProductVariantId.Value &&
                                 !s.IsDeleted,
                            cancellationToken);

                    line = new StockCountLine
                    {
                        Id = Guid.NewGuid(),
                        TenantId = _tenantContext.TenantId,
                        CountId = count.Id,
                        ProductVariantId = update.ProductVariantId.Value,
                        SystemQty = stockItem?.Quantity ?? 0,
                        CountedQty = null,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.StockCountLines.AddAsync(line, cancellationToken);
                    count.Lines.Add(line);
                }
            }

            line!.CountedQty = update.CountedQty;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
