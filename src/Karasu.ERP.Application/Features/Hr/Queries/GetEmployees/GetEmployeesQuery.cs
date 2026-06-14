using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Queries.GetEmployees;

public record GetEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    EmployeeStatus? Status = null) : IRequest<Result<PaginatedList<EmployeeListDto>>>;

public record EmployeeListDto(
    Guid Id,
    string EmployeeNo,
    string FullName,
    string? Department,
    string? Position,
    decimal Salary,
    EmployeeStatus Status,
    DateTime HireDate);

public class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<PaginatedList<EmployeeListDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetEmployeesQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<EmployeeListDto>>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.TenantId == _tenantContext.TenantId && !e.IsDeleted);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(e =>
                e.FullName.Contains(term) ||
                e.EmployeeNo.Contains(term) ||
                (e.Department != null && e.Department.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeListDto(
                e.Id, e.EmployeeNo, e.FullName, e.Department, e.Position,
                e.Salary, e.Status, e.HireDate))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<EmployeeListDto>>.Success(
            new PaginatedList<EmployeeListDto>(items, totalCount, request.Page, request.PageSize));
    }
}
