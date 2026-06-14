using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Queries.GetLeaveRequests;

public record GetLeaveRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? EmployeeId = null,
    LeaveRequestStatus? Status = null) : IRequest<Result<PaginatedList<LeaveRequestDto>>>;

public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    LeaveRequestStatus Status,
    string? Reason,
    DateTime? ApprovedAt);

public class GetLeaveRequestsQueryHandler : IRequestHandler<GetLeaveRequestsQuery, Result<PaginatedList<LeaveRequestDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetLeaveRequestsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<LeaveRequestDto>>> Handle(
        GetLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.TenantId == _tenantContext.TenantId && !l.IsDeleted);

        if (request.EmployeeId.HasValue)
            query = query.Where(l => l.EmployeeId == request.EmployeeId.Value);

        if (request.Status.HasValue)
            query = query.Where(l => l.Status == request.Status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new LeaveRequestDto(
                l.Id,
                l.EmployeeId,
                l.Employee.FullName,
                l.Type,
                l.StartDate,
                l.EndDate,
                l.Status,
                l.Reason,
                l.ApprovedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<LeaveRequestDto>>.Success(
            new PaginatedList<LeaveRequestDto>(items, totalCount, request.Page, request.PageSize));
    }
}
