using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.EInvoice.Queries.GetEInvoiceProfile;

public record GetEInvoiceProfileQuery() : IRequest<Result<EInvoiceProfileDto>>;

public record EInvoiceProfileDto(
    Guid? Id,
    EInvoiceProvider Provider,
    string? TaxNumber,
    string? CompanyTitle,
    bool IsActive,
    bool IsConfigured);

public class GetEInvoiceProfileQueryHandler : IRequestHandler<GetEInvoiceProfileQuery, Result<EInvoiceProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetEInvoiceProfileQueryHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<EInvoiceProfileDto>> Handle(
        GetEInvoiceProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _context.EInvoiceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted, cancellationToken);

        if (profile is null)
        {
            return Result<EInvoiceProfileDto>.Success(new EInvoiceProfileDto(
                null, EInvoiceProvider.Stub, null, null, false, false));
        }

        return Result<EInvoiceProfileDto>.Success(new EInvoiceProfileDto(
            profile.Id,
            profile.Provider,
            profile.TaxNumber,
            profile.CompanyTitle,
            profile.IsActive,
            profile.IsActive && !string.IsNullOrWhiteSpace(profile.TaxNumber)));
    }
}
