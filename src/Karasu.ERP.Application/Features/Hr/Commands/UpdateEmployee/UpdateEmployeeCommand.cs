using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(
    Guid Id,
    string FullName,
    string? Department,
    string? Position,
    string? Phone,
    string? Email,
    decimal Salary,
    EmployeeStatus Status) : IRequest<Result>;

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public UpdateEmployeeCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.Id &&
                                      e.TenantId == _tenantContext.TenantId &&
                                      !e.IsDeleted, cancellationToken);

        if (employee is null)
            return Result.Failure("Personel bulunamadı.", "EMPLOYEE_NOT_FOUND");

        employee.FullName = request.FullName;
        employee.Department = request.Department;
        employee.Position = request.Position;
        employee.Phone = request.Phone;
        employee.Email = request.Email;
        employee.Salary = request.Salary;
        employee.Status = request.Status;
        employee.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
