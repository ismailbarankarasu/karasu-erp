using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantNotificationPublisher _notificationPublisher;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ITenantNotificationPublisher notificationPublisher)
    {
        _orderRepository = orderRepository;
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var branchExists = await _context.Branches
            .AnyAsync(b => b.Id == request.BranchId && b.TenantId == _tenantContext.TenantId && !b.IsDeleted, cancellationToken);
        if (!branchExists)
            return Result<Guid>.Failure("Geçersiz şube.", "BRANCH_NOT_FOUND");

        if (request.CustomerId.HasValue)
        {
            var customerExists = await _context.Customers
                .AnyAsync(c => c.Id == request.CustomerId.Value && c.TenantId == _tenantContext.TenantId && !c.IsDeleted, cancellationToken);
            if (!customerExists)
                return Result<Guid>.Failure("Geçersiz müşteri.", "CUSTOMER_NOT_FOUND");
        }

        var variantIds = request.Lines.Select(l => l.ProductVariantId).Distinct().ToList();
        var existingVariantCount = await _context.ProductVariants
            .CountAsync(v => variantIds.Contains(v.Id) && v.TenantId == _tenantContext.TenantId && !v.IsDeleted, cancellationToken);
        if (existingVariantCount != variantIds.Count)
            return Result<Guid>.Failure("Geçersiz ürün varyantı.", "PRODUCT_VARIANT_NOT_FOUND");

        var orderNumber = await _orderRepository.GenerateOrderNumberAsync(cancellationToken);

        var order = Order.Create(
            _tenantContext.TenantId,
            request.BranchId,
            request.CustomerId,
            orderNumber);

        order.Notes = request.Notes;

        foreach (var line in request.Lines)
        {
            order.AddLine(
                line.ProductVariantId,
                line.Quantity,
                line.UnitPrice,
                line.TaxRate,
                line.Discount);
        }

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationPublisher.PublishToTenantAsync(
            _tenantContext.TenantId,
            "NewOrder",
            new { order.Id, order.OrderNumber, order.GrandTotal },
            cancellationToken);

        return Result<Guid>.Success(order.Id);
    }
}
