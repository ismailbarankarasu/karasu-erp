using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Queries.PrintPosReceipt;

public class PrintPosReceiptQueryHandler : IRequestHandler<PrintPosReceiptQuery, Result<PosReceiptPdfDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IReceiptPdfService _receiptPdfService;
    private readonly ITenantContext _tenantContext;

    public PrintPosReceiptQueryHandler(
        IApplicationDbContext context,
        IReceiptPdfService receiptPdfService,
        ITenantContext tenantContext)
    {
        _context = context;
        _receiptPdfService = receiptPdfService;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PosReceiptPdfDto>> Handle(
        PrintPosReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
                .ThenInclude(l => l.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(o => o.Branch)
                .ThenInclude(b => b.Tenant)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(
                o => o.Id == request.OrderId &&
                     o.TenantId == _tenantContext.TenantId &&
                     !o.IsDeleted,
                cancellationToken);

        if (order is null)
            return Result<PosReceiptPdfDto>.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Type != OrderType.Pos)
            return Result<PosReceiptPdfDto>.Failure("Sadece POS satışları için fiş yazdırılabilir.", "ORDER_NOT_POS");

        if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Cancelled)
            return Result<PosReceiptPdfDto>.Failure("Fiş yalnızca onaylı veya iade edilmiş satışlar için yazdırılabilir.", "ORDER_INVALID_STATUS");

        var payments = await _context.PosTransactions
            .AsNoTracking()
            .Where(t => t.OrderId == order.Id && !t.IsDeleted)
            .Select(t => new PosReceiptPaymentData(t.PaymentMethod, t.Amount, t.ChangeAmount))
            .ToListAsync(cancellationToken);

        var receiptData = new PosReceiptData(
            order.Branch.Tenant.Name,
            order.Branch.Name,
            order.Branch.Address,
            order.OrderNumber,
            order.CreatedAt,
            order.Customer?.FullName,
            order.Lines.Select(l => new PosReceiptLineData(
                l.ProductVariant.Product.Name,
                l.Quantity,
                l.UnitPrice,
                l.TaxRate,
                l.Discount,
                l.LineTotal)).ToList(),
            order.SubTotal,
            order.TaxTotal,
            order.DiscountTotal,
            order.GrandTotal,
            payments);

        var pdf = _receiptPdfService.Generate(receiptData);
        var fileName = $"fis-{order.OrderNumber}.pdf";

        return Result<PosReceiptPdfDto>.Success(new PosReceiptPdfDto(pdf, fileName));
    }
}
