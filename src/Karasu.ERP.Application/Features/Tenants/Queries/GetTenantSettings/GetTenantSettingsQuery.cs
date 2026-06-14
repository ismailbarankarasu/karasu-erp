using System.Text.Json;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Tenants.Queries.GetTenantSettings;

public record GetTenantSettingsQuery : IRequest<Result<Dictionary<string, JsonElement>>>;

public class GetTenantSettingsQueryHandler : IRequestHandler<GetTenantSettingsQuery, Result<Dictionary<string, JsonElement>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetTenantSettingsQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Dictionary<string, JsonElement>>> Handle(
        GetTenantSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settingsJson = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == _tenantContext.TenantId && !t.IsDeleted)
            .Select(t => t.SettingsJson)
            .FirstOrDefaultAsync(cancellationToken);

        if (settingsJson is null)
            return Result<Dictionary<string, JsonElement>>.Failure("Tenant bulunamadı.", "TENANT_NOT_FOUND");

        var settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(settingsJson)
            ?? new Dictionary<string, JsonElement>();

        return Result<Dictionary<string, JsonElement>>.Success(settings);
    }
}
