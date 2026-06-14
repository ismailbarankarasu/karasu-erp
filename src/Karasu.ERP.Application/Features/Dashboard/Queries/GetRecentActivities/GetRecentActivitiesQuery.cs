using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Dashboard.Queries.GetRecentActivities;

public record GetRecentActivitiesQuery() : IRequest<Result<List<RecentActivityDto>>>;

public record RecentActivityDto(
    string EntityType,
    string Action,
    Guid EntityId,
    Guid? UserId,
    DateTime CreatedAt);
