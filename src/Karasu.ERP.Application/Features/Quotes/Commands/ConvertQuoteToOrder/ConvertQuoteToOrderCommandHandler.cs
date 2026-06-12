using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Quotes.Commands.ConvertQuoteToOrder;

public class ConvertQuoteToOrderCommandHandler : IRequestHandler<ConvertQuoteToOrderCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantNotificationPublisher _notificationPublisher;

    public ConvertQuoteToOrderCommandHandler(
        IApplicationDbContext context,
        IOrderRepository orderRepository,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        ITenantNotificationPublisher notificationPublisher)
    {
        _context = context;
        _orderRepository = orderRepository;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<Guid>> Handle(
        ConvertQuoteToOrderCommand request,
        CancellationToken cancellationToken)
    {
        var quote = await _context.Quotes
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(
                q => q.Id == request.QuoteId && q.TenantId == _tenantContext.TenantId && !q.IsDeleted,
                cancellationToken);

        if (quote is null)
            return Result<Guid>.Failure("Teklif bulunamadı.", "QUOTE_NOT_FOUND");

        if (quote.Lines.Count == 0)
            return Result<Guid>.Failure("Teklif satırı bulunamadı.", "QUOTE_EMPTY");

        var branchExists = await _context.Branches.AnyAsync(
            b => b.Id == request.BranchId && b.TenantId == _tenantContext.TenantId && !b.IsDeleted,
            cancellationToken);
        if (!branchExists)
            return Result<Guid>.Failure("Geçersiz şube.", "BRANCH_NOT_FOUND");

        var orderNumber = await _orderRepository.GenerateOrderNumberAsync(cancellationToken);
        var order = Order.Create(_tenantContext.TenantId, request.BranchId, quote.CustomerId, orderNumber);
        order.Notes = $"Teklif: {quote.QuoteNumber}";

        foreach (var line in quote.Lines)
        {
            order.AddLine(
                line.ProductVariantId,
                line.Quantity,
                line.UnitPrice,
                line.TaxRate,
                line.Discount);
        }

        try
        {
            quote.MarkConverted(order.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ex.Message, "QUOTE_INVALID_STATUS");
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationPublisher.PublishToTenantAsync(
            _tenantContext.TenantId,
            "NewOrder",
            new { order.Id, order.OrderNumber, order.GrandTotal, FromQuote = quote.QuoteNumber },
            cancellationToken);

        return Result<Guid>.Success(order.Id);
    }
}
