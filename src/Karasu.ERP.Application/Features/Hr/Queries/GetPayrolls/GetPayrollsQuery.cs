using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Queries.GetPayrolls;

public record GetPayrollsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Period = null,
    Guid? EmployeeId = null) : IRequest<Result<PaginatedList<PayrollDto>>>;

public record PayrollDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Period,
    decimal GrossSalary,
    decimal Deductions,
    decimal NetSalary,
    PayrollStatus Status,
    DateTime? PaidAt);

public class GetPayrollsQueryHandler : IRequestHandler<GetPayrollsQuery, Result<PaginatedList<PayrollDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetPayrollsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PaginatedList<PayrollDto>>> Handle(
        GetPayrollsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Payrolls
            .AsNoTracking()
            .Where(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Period))
            query = query.Where(p => p.Period == request.Period);

        if (request.EmployeeId.HasValue)
            query = query.Where(p => p.EmployeeId == request.EmployeeId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.Period)
            .ThenBy(p => p.Employee.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PayrollDto(
                p.Id,
                p.EmployeeId,
                p.Employee.FullName,
                p.Period,
                p.GrossSalary,
                p.Deductions,
                p.NetSalary,
                p.Status,
                p.PaidAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<PayrollDto>>.Success(
            new PaginatedList<PayrollDto>(items, totalCount, request.Page, request.PageSize));
    }
}
