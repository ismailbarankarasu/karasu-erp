using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsRead = null) : IRequest<Result<PaginatedList<NotificationDto>>>;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<PaginatedList<NotificationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICurrentUserService currentUser)
    {
        _context = context;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PaginatedList<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.TenantId == _tenantContext.TenantId && !n.IsDeleted);

        if (userId.HasValue)
            query = query.Where(n => n.UserId == null || n.UserId == userId.Value);

        if (request.IsRead.HasValue)
            query = query.Where(n => n.IsRead == request.IsRead.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt, n.ReadAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<NotificationDto>>.Success(
            new PaginatedList<NotificationDto>(items, totalCount, request.Page, request.PageSize));
    }
}
