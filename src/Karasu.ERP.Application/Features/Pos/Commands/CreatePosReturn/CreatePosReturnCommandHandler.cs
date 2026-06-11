using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Commands.CreatePosReturn;

public class CreatePosReturnCommandHandler : IRequestHandler<CreatePosReturnCommand, Result<PosReturnResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IStockService _stockService;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public CreatePosReturnCommandHandler(
        IApplicationDbContext context,
        IStockService stockService,
        ICurrentUserService currentUser,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _context = context;
        _stockService = stockService;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<Result<PosReturnResultDto>> Handle(
        CreatePosReturnCommand request,
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
            return Result<PosReturnResultDto>.Failure("Açık kasa oturumu bulunamadı.", "POS_SESSION_NOT_FOUND");

        if (session.CashierId != _currentUser.UserId)
            return Result<PosReturnResultDto>.Failure("Bu kasa oturumunda iade yapma yetkiniz yok.", "FORBIDDEN");

        var alreadyReturned = await _context.PosReturns.AnyAsync(
            r => r.OriginalOrderId == request.OriginalOrderId && !r.IsDeleted,
            cancellationToken);

        if (alreadyReturned)
            return Result<PosReturnResultDto>.Failure("Bu sipariş için iade zaten yapılmış.", "POS_RETURN_ALREADY_EXISTS");

        var order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(
                o => o.Id == request.OriginalOrderId &&
                     o.TenantId == _tenantContext.TenantId &&
                     !o.IsDeleted,
                cancellationToken);

        if (order is null)
            return Result<PosReturnResultDto>.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Status != OrderStatus.Confirmed)
            return Result<PosReturnResultDto>.Failure("Sadece onaylı siparişler iade edilebilir.", "ORDER_INVALID_STATUS");

        if (order.Type != OrderType.Pos)
            return Result<PosReturnResultDto>.Failure("Sadece POS satışları iade edilebilir.", "ORDER_NOT_POS");

        var hasSessionSale = await _context.PosTransactions.AnyAsync(
            t => t.SessionId == session.Id && t.OrderId == order.Id && !t.IsDeleted,
            cancellationToken);

        if (!hasSessionSale)
            return Result<PosReturnResultDto>.Failure("Bu sipariş bu kasa oturumuna ait değil.", "ORDER_NOT_IN_SESSION");

        var warehouseId = await _stockService.GetDefaultWarehouseIdForBranchAsync(order.BranchId, cancellationToken);
        if (warehouseId is null)
            return Result<PosReturnResultDto>.Failure("Depo bulunamadı.", "WAREHOUSE_NOT_FOUND");

        var stockLines = order.Lines
            .Select(l => new StockOrderLine(l.ProductVariantId, l.Quantity))
            .ToList();

        var stockResult = await _stockService.RestoreForOrderAsync(
            warehouseId.Value, order.Id, stockLines, cancellationToken);

        if (!stockResult.IsSuccess)
            return Result<PosReturnResultDto>.Failure(stockResult.Error!, stockResult.ErrorCode);

        try
        {
            order.Cancel(_currentUser.UserId ?? Guid.Empty, request.Reason ?? "POS iadesi");
        }
        catch (InvalidOperationException ex)
        {
            return Result<PosReturnResultDto>.Failure(ex.Message, "ORDER_INVALID_STATUS");
        }

        _context.OrderStatusHistories.Add(order.StatusHistory.Last());

        if (request.RefundMethod == PaymentMethod.Credit && order.CustomerId.HasValue)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == order.CustomerId.Value, cancellationToken);

            if (customer is not null)
                customer.Balance -= order.GrandTotal;
        }

        var posReturn = new PosReturn
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            SessionId = session.Id,
            OriginalOrderId = order.Id,
            Reason = request.Reason,
            RefundAmount = order.GrandTotal,
            RefundMethod = request.RefundMethod,
            CreatedAt = DateTime.UtcNow
        };

        await _context.PosReturns.AddAsync(posReturn, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync("dashboard:*", cancellationToken);
        await _cacheService.RemoveByPatternAsync($"{_tenantContext.TenantId}:stock:item:*", cancellationToken);

        return Result<PosReturnResultDto>.Success(new PosReturnResultDto(
            posReturn.Id,
            order.Id,
            posReturn.RefundAmount,
            posReturn.RefundMethod));
    }
}
