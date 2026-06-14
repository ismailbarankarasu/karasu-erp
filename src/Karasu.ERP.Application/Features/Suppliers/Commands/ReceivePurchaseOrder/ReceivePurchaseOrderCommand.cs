using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Suppliers.Commands.ReceivePurchaseOrder;

public record ReceivePurchaseOrderLineRequest(Guid LineId, decimal Quantity);

public record ReceivePurchaseOrderCommand(
    Guid PurchaseOrderId,
    Guid WarehouseId,
    IReadOnlyList<ReceivePurchaseOrderLineRequest> Lines) : IRequest<Result>;

public class ReceivePurchaseOrderCommandHandler : IRequestHandler<ReceivePurchaseOrderCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public ReceivePurchaseOrderCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IStockService stockService)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<Result> Handle(ReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            return Result.Failure("En az bir satır kabul edilmelidir.", "LINES_REQUIRED");

        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId &&
                           w.TenantId == _tenantContext.TenantId &&
                           !w.IsDeleted, cancellationToken);
        if (!warehouseExists)
            return Result.Failure("Geçersiz depo.", "WAREHOUSE_NOT_FOUND");

        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId &&
                                      p.TenantId == _tenantContext.TenantId &&
                                      !p.IsDeleted, cancellationToken);

        if (po is null)
            return Result.Failure("Satın alma siparişi bulunamadı.", "PO_NOT_FOUND");

        var receipts = request.Lines
            .Select(l => (l.LineId, l.Quantity))
            .ToList();

        try
        {
            po.Receive(receipts);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "PO_RECEIVE_FAILED");
        }

        decimal receivedValue = 0;
        foreach (var receipt in request.Lines.Where(l => l.Quantity > 0))
        {
            var line = po.Lines.First(l => l.Id == receipt.LineId);
            var unitValue = line.UnitPrice * (1 + line.TaxRate / 100);
            receivedValue += receipt.Quantity * unitValue;

            var stockResult = await _stockService.AdjustStockAsync(
                request.WarehouseId,
                line.ProductVariantId,
                receipt.Quantity,
                $"PO mal kabul: {po.PoNumber}",
                cancellationToken,
                "PurchaseOrder",
                po.Id);

            if (!stockResult.IsSuccess)
                return stockResult;
        }

        if (receivedValue > 0)
        {
            po.Supplier.Balance += receivedValue;

            var payable = new Payable
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                CreditorName = po.Supplier.Name,
                SupplierId = po.SupplierId,
                Amount = receivedValue,
                DueDate = DateTime.UtcNow.AddDays(30),
                Status = PayableStatus.Open,
                Description = $"PO mal kabul: {po.PoNumber}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Payables.Add(payable);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
