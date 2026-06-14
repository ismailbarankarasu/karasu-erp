using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand() : IRequest<Result<int>>;

public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public MarkAllNotificationsReadCommandHandler(
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

    public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .Where(n => n.TenantId == _tenantContext.TenantId && !n.IsDeleted && !n.IsRead);

        if (_currentUser.UserId.HasValue)
            query = query.Where(n => n.UserId == null || n.UserId == _currentUser.UserId.Value);

        var notifications = await query.ToListAsync(cancellationToken);
        foreach (var notification in notifications)
            notification.MarkRead();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(notifications.Count);
    }
}
