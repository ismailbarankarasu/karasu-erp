using System.Text.Json;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Tenants.Commands.UpdateTenantSettings;

public record UpdateTenantSettingsCommand(Dictionary<string, JsonElement> Settings) : IRequest<Result>;

public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantSettingsCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateTenantSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId && !t.IsDeleted, cancellationToken);

        if (tenant is null)
            return Result.Failure("Tenant bulunamadı.", "TENANT_NOT_FOUND");

        tenant.SettingsJson = JsonSerializer.Serialize(request.Settings);
        tenant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
