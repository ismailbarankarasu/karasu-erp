using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid Id) : IRequest<Result>;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
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

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id &&
                                      n.TenantId == _tenantContext.TenantId &&
                                      !n.IsDeleted, cancellationToken);

        if (notification is null)
            return Result.Failure("Bildirim bulunamadı.", "NOTIFICATION_NOT_FOUND");

        if (_currentUser.UserId.HasValue &&
            notification.UserId.HasValue &&
            notification.UserId != _currentUser.UserId)
            return Result.Failure("Bu bildirime erişim yetkiniz yok.", "FORBIDDEN");

        notification.MarkRead();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
