using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Domain.Events;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Commands.CreatePosSale;

public class CreatePosSaleCommandHandler : IRequestHandler<CreatePosSaleCommand, Result<PosSaleResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IStockService _stockService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ITenantNotificationPublisher _notificationPublisher;
    private readonly IMediator _mediator;

    public CreatePosSaleCommandHandler(
        IApplicationDbContext context,
        IOrderRepository orderRepository,
        IStockService stockService,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ITenantNotificationPublisher notificationPublisher,
        IMediator mediator)
    {
        _context = context;
        _orderRepository = orderRepository;
        _stockService = stockService;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _notificationPublisher = notificationPublisher;
        _mediator = mediator;
    }

    public async Task<Result<PosSaleResultDto>> Handle(
        CreatePosSaleCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _context.PosSessions
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId &&
                     s.TenantId == _tenantContext.TenantId &&
                     s.Status == PosSessionStatus.Open &&
                     !s.IsDeleted,
                cancellationToken);

        if (session is null)
            return Result<PosSaleResultDto>.Failure("Açık kasa oturumu bulunamadı.", "POS_SESSION_NOT_FOUND");

        if (session.CashierId != _currentUser.UserId)
            return Result<PosSaleResultDto>.Failure("Bu kasa oturumuna satış yapma yetkiniz yok.", "FORBIDDEN");

        if (request.CustomerId.HasValue)
        {
            var customerExists = await _context.Customers.AnyAsync(
                c => c.Id == request.CustomerId.Value &&
                     c.TenantId == _tenantContext.TenantId &&
                     !c.IsDeleted,
                cancellationToken);

            if (!customerExists)
                return Result<PosSaleResultDto>.Failure("Geçersiz müşteri.", "CUSTOMER_NOT_FOUND");
        }

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var existingVariantCount = await _context.ProductVariants.CountAsync(
            v => variantIds.Contains(v.Id) && v.TenantId == _tenantContext.TenantId && !v.IsDeleted,
            cancellationToken);

        if (existingVariantCount != variantIds.Count)
            return Result<PosSaleResultDto>.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        var orderNumber = await _orderRepository.GenerateOrderNumberAsync(cancellationToken);

        var order = Order.Create(
            _tenantContext.TenantId,
            session.BranchId,
            request.CustomerId,
            orderNumber);

        order.Type = OrderType.Pos;
        order.Notes = $"POS:{session.Id}";

        foreach (var line in request.Lines)
        {
            order.AddLine(
                line.ProductVariantId,
                line.Quantity,
                line.UnitPrice,
                line.TaxRate,
                line.Discount);
        }

        var paymentTotal = request.Payments.Sum(p => p.Amount);
        if (paymentTotal != order.GrandTotal)
        {
            return Result<PosSaleResultDto>.Failure(
                $"Ödeme tutarı ({paymentTotal:N2}) sipariş toplamına ({order.GrandTotal:N2}) eşit olmalıdır.",
                "PAYMENT_MISMATCH");
        }

        try
        {
            order.Confirm(_currentUser.UserId ?? Guid.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return Result<PosSaleResultDto>.Failure(ex.Message, "ORDER_INVALID_STATUS");
        }

        var warehouseId = await _stockService.GetDefaultWarehouseIdForBranchAsync(session.BranchId, cancellationToken);
        if (warehouseId is null)
            return Result<PosSaleResultDto>.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        var stockLines = order.Lines
            .Select(l => new StockOrderLine(l.ProductVariantId, l.Quantity))
            .ToList();

        var stockResult = await _stockService.DeductForOrderAsync(
            warehouseId.Value, order.Id, stockLines, cancellationToken);

        if (!stockResult.IsSuccess)
            return Result<PosSaleResultDto>.Failure(stockResult.Error!, stockResult.ErrorCode);

        await _orderRepository.AddAsync(order, cancellationToken);
        _context.OrderStatusHistories.Add(order.StatusHistory.Last());

        foreach (var payment in request.Payments)
        {
            await _context.PosTransactions.AddAsync(new PosTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                SessionId = session.Id,
                OrderId = order.Id,
                PaymentMethod = payment.Method,
                Amount = payment.Amount,
                ChangeAmount = payment.ChangeAmount,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var domainEvent = new OrderConfirmedEvent(
            order.Id,
            order.TenantId,
            order.BranchId,
            order.Lines.Select(l => new OrderLineSnapshot(l.ProductVariantId, l.Quantity, l.UnitPrice)).ToList());

        await _mediator.Publish(domainEvent, cancellationToken);

        await _cacheService.RemoveByPatternAsync("dashboard:*", cancellationToken);
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:stock:item:*", cancellationToken);

        await _notificationPublisher.PublishToTenantAsync(
            _tenantContext.TenantId,
            "PosSaleCompleted",
            new { OrderId = order.Id, order.OrderNumber, order.GrandTotal, SessionId = session.Id },
            cancellationToken);

        return Result<PosSaleResultDto>.Success(new PosSaleResultDto(
            order.Id,
            order.OrderNumber,
            order.GrandTotal,
            request.Payments.Count));
    }
}
