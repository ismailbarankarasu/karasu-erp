using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Invoices.Commands.CreateInvoiceFromOrder;

public class CreateInvoiceFromOrderCommandHandler : IRequestHandler<CreateInvoiceFromOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInvoiceFromOrderCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        CreateInvoiceFromOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Lines)
                .ThenInclude(l => l.ProductVariant)
                .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(
                o => o.Id == request.OrderId &&
                     o.TenantId == _tenantContext.TenantId &&
                     !o.IsDeleted,
                cancellationToken);

        if (order is null)
            return Result<Guid>.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Draft)
            return Result<Guid>.Failure("Bu sipariş için fatura oluşturulamaz.", "ORDER_INVALID_STATUS");

        if (!order.CustomerId.HasValue)
            return Result<Guid>.Failure("Fatura için müşteri gereklidir.", "CUSTOMER_REQUIRED");

        var existingInvoice = await _context.Invoices.AnyAsync(
            i => i.OrderId == order.Id && i.Status != InvoiceStatus.Cancelled && !i.IsDeleted,
            cancellationToken);

        if (existingInvoice)
            return Result<Guid>.Failure("Bu sipariş için zaten fatura mevcut.", "INVOICE_ALREADY_EXISTS");

        var invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken);
        var invoice = Invoice.CreateFromOrder(
            _tenantContext.TenantId,
            order,
            order.CustomerId.Value,
            invoiceNumber,
            request.Type);

        foreach (var line in invoice.Lines)
        {
            if (line.ProductVariantId.HasValue)
            {
                var orderLine = order.Lines.First(l => l.ProductVariantId == line.ProductVariantId);
                line.Description = orderLine.ProductVariant.Product.Name;
            }
        }

        if (request.IssueImmediately)
        {
            try
            {
                invoice.Issue();
            }
            catch (InvalidOperationException ex)
            {
                return Result<Guid>.Failure(ex.Message, "INVOICE_INVALID_STATUS");
            }
        }

        await _context.Invoices.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }

    private async Task<string> GenerateInvoiceNumberAsync(CancellationToken ct)
    {
        var count = await _context.Invoices.CountAsync(ct);
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D5}";
    }
}
