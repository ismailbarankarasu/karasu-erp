using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Commands.CreateShift;

public record CreateShiftCommand(
    Guid EmployeeId,
    Guid BranchId,
    DateTime Date,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string? Notes) : IRequest<Result<Guid>>;

public class CreateShiftCommandHandler : IRequestHandler<CreateShiftCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreateShiftCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateShiftCommand request, CancellationToken cancellationToken)
    {
        if (request.EndTime <= request.StartTime)
            return Result<Guid>.Failure("Bitiş saati başlangıçtan sonra olmalıdır.", "INVALID_SHIFT_TIME");

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId &&
                           e.TenantId == _tenantContext.TenantId &&
                           !e.IsDeleted, cancellationToken);
        if (!employeeExists)
            return Result<Guid>.Failure("Personel bulunamadı.", "EMPLOYEE_NOT_FOUND");

        var branchExists = await _context.Branches
            .AnyAsync(b => b.Id == request.BranchId &&
                           b.TenantId == _tenantContext.TenantId &&
                           !b.IsDeleted, cancellationToken);
        if (!branchExists)
            return Result<Guid>.Failure("Geçersiz şube.", "BRANCH_NOT_FOUND");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            EmployeeId = request.EmployeeId,
            BranchId = request.BranchId,
            Date = request.Date.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shifts.Add(shift);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(shift.Id);
    }
}
