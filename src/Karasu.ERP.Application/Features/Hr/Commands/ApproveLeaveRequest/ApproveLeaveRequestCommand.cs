using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Commands.ApproveLeaveRequest;

public record ApproveLeaveRequestCommand(Guid Id, bool Approve) : IRequest<Result>;

public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public ApproveLeaveRequestCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ICurrentUserService currentUser)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leave = await _context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == request.Id &&
                                      l.TenantId == _tenantContext.TenantId &&
                                      !l.IsDeleted, cancellationToken);

        if (leave is null)
            return Result.Failure("İzin talebi bulunamadı.", "LEAVE_NOT_FOUND");

        var approverId = _currentUser.UserId;
        if (!approverId.HasValue)
            return Result.Failure("Kullanıcı kimliği bulunamadı.", "UNAUTHORIZED");

        try
        {
            if (request.Approve)
                leave.Approve(approverId.Value);
            else
                leave.Reject(approverId.Value);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVALID_LEAVE_STATUS");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
