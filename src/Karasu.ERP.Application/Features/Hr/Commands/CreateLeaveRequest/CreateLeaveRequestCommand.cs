using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Commands.CreateLeaveRequest;

public record CreateLeaveRequestCommand(
    Guid EmployeeId,
    LeaveType Type,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason) : IRequest<Result<Guid>>;

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreateLeaveRequestCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate < request.StartDate)
            return Result<Guid>.Failure("Bitiş tarihi başlangıçtan önce olamaz.", "INVALID_DATE_RANGE");

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId &&
                           e.TenantId == _tenantContext.TenantId &&
                           !e.IsDeleted, cancellationToken);
        if (!employeeExists)
            return Result<Guid>.Failure("Personel bulunamadı.", "EMPLOYEE_NOT_FOUND");

        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            Reason = request.Reason,
            Status = LeaveRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leave);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(leave.Id);
    }
}
