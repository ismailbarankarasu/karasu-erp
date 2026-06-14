using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Hr.Commands.GeneratePayroll;

public record GeneratePayrollCommand(
    string Period,
    decimal DefaultDeductionRate = 0.15m) : IRequest<Result<GeneratePayrollResult>>;

public record GeneratePayrollResult(int GeneratedCount, int SkippedCount);

public class GeneratePayrollCommandHandler : IRequestHandler<GeneratePayrollCommand, Result<GeneratePayrollResult>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public GeneratePayrollCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GeneratePayrollResult>> Handle(
        GeneratePayrollCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Period))
            return Result<GeneratePayrollResult>.Failure("Dönem belirtilmelidir (örn. 2026-06).", "PERIOD_REQUIRED");

        var employees = await _context.Employees
            .Where(e => e.TenantId == _tenantContext.TenantId &&
                        !e.IsDeleted &&
                        e.Status == EmployeeStatus.Active)
            .ToListAsync(cancellationToken);

        var existingEmployeeIds = await _context.Payrolls
            .Where(p => p.TenantId == _tenantContext.TenantId &&
                        p.Period == request.Period &&
                        !p.IsDeleted)
            .Select(p => p.EmployeeId)
            .ToListAsync(cancellationToken);

        var generated = 0;
        var skipped = 0;

        foreach (var employee in employees)
        {
            if (existingEmployeeIds.Contains(employee.Id))
            {
                skipped++;
                continue;
            }

            var deductions = Math.Round(employee.Salary * request.DefaultDeductionRate, 4);
            var payroll = Payroll.Generate(
                _tenantContext.TenantId,
                employee.Id,
                request.Period,
                employee.Salary,
                deductions);

            _context.Payrolls.Add(payroll);
            generated++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GeneratePayrollResult>.Success(new GeneratePayrollResult(generated, skipped));
    }
}
