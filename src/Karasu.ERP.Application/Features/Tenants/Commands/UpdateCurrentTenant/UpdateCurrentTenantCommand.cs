using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Tenants.Commands.UpdateCurrentTenant;

public record UpdateCurrentTenantCommand(
    string Name,
    BusinessType BusinessType,
    SubscriptionPlan Plan) : IRequest<Result>;

public class UpdateCurrentTenantCommandHandler : IRequestHandler<UpdateCurrentTenantCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCurrentTenantCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateCurrentTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId && !t.IsDeleted, cancellationToken);

        if (tenant is null)
            return Result.Failure("Tenant bulunamadı.", "TENANT_NOT_FOUND");

        tenant.Name = request.Name.Trim();
        tenant.BusinessType = request.BusinessType;
        tenant.Plan = request.Plan;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
