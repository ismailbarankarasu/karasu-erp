using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Commands.ChangeOrderStatus;

public class ChangeOrderStatusCommandHandler : IRequestHandler<ChangeOrderStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeOrderStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(
                o => o.Id == request.Id && o.TenantId == _tenantContext.TenantId && !o.IsDeleted,
                cancellationToken);

        if (order is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Status != OrderStatus.Confirmed &&
            order.Status != OrderStatus.Preparing &&
            order.Status != OrderStatus.Shipping)
            return Result.Failure("Sipariş durumu değiştirilemez.", "ORDER_INVALID_STATUS");

        try
        {
            order.ChangeStatus(request.Status, _currentUser.UserId ?? Guid.Empty, request.Note);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "ORDER_INVALID_STATUS");
        }

        _context.OrderStatusHistories.Add(order.StatusHistory.Last());
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
