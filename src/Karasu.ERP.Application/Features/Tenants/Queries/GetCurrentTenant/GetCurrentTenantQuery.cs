using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Tenants.Queries.GetCurrentTenant;

public record GetCurrentTenantQuery : IRequest<Result<CurrentTenantDto>>;

public record CurrentTenantDto(
    Guid Id,
    string Name,
    string Slug,
    BusinessType BusinessType,
    SubscriptionPlan Plan,
    bool IsActive,
    DateTime CreatedAt);

public class GetCurrentTenantQueryHandler : IRequestHandler<GetCurrentTenantQuery, Result<CurrentTenantDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetCurrentTenantQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CurrentTenantDto>> Handle(GetCurrentTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == _tenantContext.TenantId && !t.IsDeleted)
            .Select(t => new CurrentTenantDto(
                t.Id,
                t.Name,
                t.Slug,
                t.BusinessType,
                t.Plan,
                t.IsActive,
                t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return tenant is null
            ? Result<CurrentTenantDto>.Failure("Tenant bulunamadı.", "TENANT_NOT_FOUND")
            : Result<CurrentTenantDto>.Success(tenant);
    }
}
