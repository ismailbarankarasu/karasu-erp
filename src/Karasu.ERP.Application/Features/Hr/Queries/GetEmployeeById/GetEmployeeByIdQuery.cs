using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<Result<EmployeeDetailDto>>;

public record EmployeeDetailDto(
    Guid Id,
    string EmployeeNo,
    string FullName,
    string? Department,
    string? Position,
    string? Phone,
    string? Email,
    DateTime HireDate,
    decimal Salary,
    EmployeeStatus Status,
    Guid? UserId);

public class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetEmployeeByIdQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<EmployeeDetailDto>> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.Id && e.TenantId == _tenantContext.TenantId && !e.IsDeleted)
            .Select(e => new EmployeeDetailDto(
                e.Id, e.EmployeeNo, e.FullName, e.Department, e.Position,
                e.Phone, e.Email, e.HireDate, e.Salary, e.Status, e.UserId))
            .FirstOrDefaultAsync(cancellationToken);

        return employee is null
            ? Result<EmployeeDetailDto>.Failure("Personel bulunamadı.", "EMPLOYEE_NOT_FOUND")
            : Result<EmployeeDetailDto>.Success(employee);
    }
}
