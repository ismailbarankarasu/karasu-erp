using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Commands.CreateEmployee;

public record CreateEmployeeCommand(
    string EmployeeNo,
    string FullName,
    string? Department,
    string? Position,
    string? Phone,
    string? Email,
    DateTime HireDate,
    decimal Salary,
    Guid? UserId = null) : IRequest<Result<Guid>>;

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreateEmployeeCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Employees
            .AnyAsync(e => e.TenantId == _tenantContext.TenantId &&
                           e.EmployeeNo == request.EmployeeNo &&
                           !e.IsDeleted, cancellationToken);
        if (exists)
            return Result<Guid>.Failure("Bu personel numarası zaten kayıtlı.", "EMPLOYEE_NO_EXISTS");

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            UserId = request.UserId,
            EmployeeNo = request.EmployeeNo,
            FullName = request.FullName,
            Department = request.Department,
            Position = request.Position,
            Phone = request.Phone,
            Email = request.Email,
            HireDate = request.HireDate.Date,
            Salary = request.Salary,
            Status = EmployeeStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(employee.Id);
    }
}
