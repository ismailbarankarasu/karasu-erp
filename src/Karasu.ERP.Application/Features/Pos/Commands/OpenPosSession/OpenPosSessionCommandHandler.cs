using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Pos.Commands.OpenPosSession;

public class OpenPosSessionCommandHandler : IRequestHandler<OpenPosSessionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public OpenPosSessionCommandHandler(
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

    public async Task<Result<Guid>> Handle(OpenPosSessionCommand request, CancellationToken cancellationToken)
    {
        var cashierId = _currentUser.UserId;
        if (cashierId is null)
            return Result<Guid>.Failure("Kullanıcı oturumu bulunamadı.", "UNAUTHORIZED");

        var branchExists = await _context.Branches.AnyAsync(
            b => b.Id == request.BranchId && b.TenantId == _tenantContext.TenantId && !b.IsDeleted,
            cancellationToken);

        if (!branchExists)
            return Result<Guid>.Failure("Geçersiz şube.", "BRANCH_NOT_FOUND");

        var hasOpenSession = await _context.PosSessions.AnyAsync(
            s => s.CashierId == cashierId &&
                 s.TenantId == _tenantContext.TenantId &&
                 s.Status == PosSessionStatus.Open &&
                 !s.IsDeleted,
            cancellationToken);

        if (hasOpenSession)
            return Result<Guid>.Failure("Zaten açık bir kasa oturumunuz var.", "POS_SESSION_ALREADY_OPEN");

        var session = PosSession.Open(
            _tenantContext.TenantId,
            request.BranchId,
            cashierId.Value,
            request.OpeningBalance);

        await _context.PosSessions.AddAsync(session, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(session.Id);
    }
}
