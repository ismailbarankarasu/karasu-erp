using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.EInvoice.Commands.UpdateEInvoiceProfile;

public record UpdateEInvoiceProfileCommand(
    EInvoiceProvider Provider,
    string? ApiKey,
    string? ApiSecret,
    string? CertificatePath,
    string? TaxNumber,
    string? CompanyTitle,
    bool IsActive,
    string? SettingsJson) : IRequest<Result<Guid>>;

public class UpdateEInvoiceProfileCommandHandler : IRequestHandler<UpdateEInvoiceProfileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public UpdateEInvoiceProfileCommandHandler(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(UpdateEInvoiceProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _context.EInvoiceProfiles
            .FirstOrDefaultAsync(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted, cancellationToken);

        if (profile is null)
        {
            profile = new EInvoiceProfile
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow
            };
            _context.EInvoiceProfiles.Add(profile);
        }

        profile.Provider = request.Provider;
        profile.ApiKey = request.ApiKey;
        profile.ApiSecret = request.ApiSecret;
        profile.CertificatePath = request.CertificatePath;
        profile.TaxNumber = request.TaxNumber;
        profile.CompanyTitle = request.CompanyTitle;
        profile.IsActive = request.IsActive;
        profile.SettingsJson = string.IsNullOrWhiteSpace(request.SettingsJson) ? "{}" : request.SettingsJson;
        profile.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(profile.Id);
    }
}
