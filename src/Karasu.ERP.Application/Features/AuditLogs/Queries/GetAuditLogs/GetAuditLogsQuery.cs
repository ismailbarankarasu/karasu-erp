using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    Guid? EntityId = null,
    Guid? UserId = null) : IRequest<Result<IReadOnlyList<AuditLogDto>>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<IReadOnlyList<AuditLogDto>>>
{
    private readonly IRoleRepository _roles;

    public GetAuditLogsQueryHandler(IRoleRepository roles) => _roles = roles;

    public async Task<Result<IReadOnlyList<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken ct)
    {
        var logs = await _roles.GetAuditLogsAsync(
            new AuditLogFilter(request.Page, request.PageSize, request.EntityType, request.EntityId, request.UserId), ct);

        return Result<IReadOnlyList<AuditLogDto>>.Success(logs);
    }
}
