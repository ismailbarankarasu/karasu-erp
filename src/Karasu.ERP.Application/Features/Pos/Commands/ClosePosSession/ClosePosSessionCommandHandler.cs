using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Commands.ClosePosSession;

public class ClosePosSessionCommandHandler : IRequestHandler<ClosePosSessionCommand, Result<PosSessionCloseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public ClosePosSessionCommandHandler(
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

    public async Task<Result<PosSessionCloseDto>> Handle(
        ClosePosSessionCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _context.PosSessions
            .FirstOrDefaultAsync(
                s => s.Id == request.SessionId &&
                     s.TenantId == _tenantContext.TenantId &&
                     !s.IsDeleted,
                cancellationToken);

        if (session is null)
            return Result<PosSessionCloseDto>.Failure("Kasa oturumu bulunamadı.", "POS_SESSION_NOT_FOUND");

        if (session.CashierId != _currentUser.UserId)
            return Result<PosSessionCloseDto>.Failure("Bu oturumu kapatma yetkiniz yok.", "FORBIDDEN");

        if (session.Status == PosSessionStatus.Closed)
            return Result<PosSessionCloseDto>.Failure("Kasa oturumu zaten kapatılmış.", "POS_SESSION_ALREADY_CLOSED");

        var transactions = await _context.PosTransactions
            .AsNoTracking()
            .Where(t => t.SessionId == session.Id && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var returns = await _context.PosReturns
            .AsNoTracking()
            .Where(r => r.SessionId == session.Id && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalSales = transactions.Sum(t => t.Amount);
        var totalRefunds = returns.Sum(r => r.RefundAmount);
        var cashReceived = transactions
            .Where(t => t.PaymentMethod == PaymentMethod.Cash)
            .Sum(t => t.Amount);
        var cashChange = transactions
            .Where(t => t.PaymentMethod == PaymentMethod.Cash)
            .Sum(t => t.ChangeAmount);
        var cashRefunds = returns
            .Where(r => r.RefundMethod == PaymentMethod.Cash)
            .Sum(r => r.RefundAmount);
        var expectedCash = session.OpeningBalance + cashReceived - cashChange - cashRefunds;
        var cashVariance = request.ClosingBalance - expectedCash;

        try
        {
            session.Close(request.ClosingBalance);
        }
        catch (InvalidOperationException ex)
        {
            return Result<PosSessionCloseDto>.Failure(ex.Message, "POS_SESSION_ALREADY_CLOSED");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<PosSessionCloseDto>.Success(new PosSessionCloseDto(
            session.Id,
            session.OpeningBalance,
            request.ClosingBalance,
            totalSales,
            totalRefunds,
            expectedCash,
            cashVariance,
            transactions.Select(t => t.OrderId).Distinct().Count(),
            returns.Count));
    }
}
