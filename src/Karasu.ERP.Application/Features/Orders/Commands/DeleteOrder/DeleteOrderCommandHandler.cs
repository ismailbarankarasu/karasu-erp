using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Orders.Commands.DeleteOrder;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOrderCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(
                o => o.Id == request.Id && o.TenantId == _tenantContext.TenantId && !o.IsDeleted,
                cancellationToken);

        if (order is null)
            return Result.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Status != OrderStatus.Draft)
            return Result.Failure("Sadece taslak siparişler silinebilir.", "ORDER_INVALID_STATUS");

        if (order.Type == OrderType.Pos)
            return Result.Failure("POS siparişleri silinemez.", "ORDER_NOT_DELETABLE");

        order.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
