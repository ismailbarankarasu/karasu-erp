using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Queries.GetShifts;

public record GetShiftsQuery(
    Guid? BranchId = null,
    DateTime? From = null,
    DateTime? To = null,
    Guid? EmployeeId = null) : IRequest<Result<List<ShiftDto>>>;

public record ShiftDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid BranchId,
    string BranchName,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Notes);

public class GetShiftsQueryHandler : IRequestHandler<GetShiftsQuery, Result<List<ShiftDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetShiftsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<List<ShiftDto>>> Handle(GetShiftsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Shifts
            .AsNoTracking()
            .Where(s => s.TenantId == _tenantContext.TenantId && !s.IsDeleted);

        if (request.BranchId.HasValue)
            query = query.Where(s => s.BranchId == request.BranchId.Value);

        if (request.EmployeeId.HasValue)
            query = query.Where(s => s.EmployeeId == request.EmployeeId.Value);

        if (request.From.HasValue)
            query = query.Where(s => s.Date >= request.From.Value.Date);

        if (request.To.HasValue)
            query = query.Where(s => s.Date <= request.To.Value.Date);

        var items = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new ShiftDto(
                s.Id,
                s.EmployeeId,
                s.Employee.FullName,
                s.BranchId,
                s.Branch.Name,
                s.Date,
                s.StartTime,
                s.EndTime,
                s.Notes))
            .ToListAsync(cancellationToken);

        return Result<List<ShiftDto>>.Success(items);
    }
}
