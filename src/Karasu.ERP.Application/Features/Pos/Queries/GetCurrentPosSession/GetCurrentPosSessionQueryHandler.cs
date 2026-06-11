using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Queries.GetCurrentPosSession;

public class GetCurrentPosSessionQueryHandler : IRequestHandler<GetCurrentPosSessionQuery, Result<CurrentPosSessionDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;

    public GetCurrentPosSessionQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ITenantContext tenantContext)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CurrentPosSessionDto?>> Handle(
        GetCurrentPosSessionQuery request,
        CancellationToken cancellationToken)
    {
        var cashierId = _currentUser.UserId;
        if (cashierId is null)
            return Result<CurrentPosSessionDto?>.Failure("Kullanıcı oturumu bulunamadı.", "UNAUTHORIZED");

        var session = await _context.PosSessions
            .AsNoTracking()
            .Where(s =>
                s.CashierId == cashierId &&
                s.TenantId == _tenantContext.TenantId &&
                s.Status == PosSessionStatus.Open &&
                !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.BranchId,
                BranchName = s.Branch.Name,
                s.OpenedAt,
                s.OpeningBalance,
                s.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
            return Result<CurrentPosSessionDto?>.Success(null);

        var transactions = await _context.PosTransactions
            .AsNoTracking()
            .Where(t => t.SessionId == session.Id && !t.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalSales = transactions.Sum(t => t.Amount);
        var cashSales = transactions
            .Where(t => t.PaymentMethod == PaymentMethod.Cash)
            .Sum(t => t.Amount - t.ChangeAmount);

        return Result<CurrentPosSessionDto?>.Success(new CurrentPosSessionDto(
            session.Id,
            session.BranchId,
            session.BranchName,
            session.OpenedAt,
            session.OpeningBalance,
            session.Status,
            transactions.Select(t => t.OrderId).Distinct().Count(),
            totalSales,
            cashSales));
    }
}
