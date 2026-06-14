using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.EInvoice.Queries.GetSubmissionStatus;

public record GetSubmissionStatusQuery(Guid Id) : IRequest<Result<SubmissionStatusDto>>;

public record SubmissionStatusDto(
    Guid Id,
    EInvoiceSubmissionStatus Status,
    string? GibUuid,
    string? ResponseJson,
    string? ErrorMessage);

public class GetSubmissionStatusQueryHandler : IRequestHandler<GetSubmissionStatusQuery, Result<SubmissionStatusDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IEInvoiceProviderResolver _providerResolver;

    public GetSubmissionStatusQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IEInvoiceProviderResolver providerResolver)
    {
        _context = context;
        _tenantContext = tenantContext;
        _providerResolver = providerResolver;
    }

    public async Task<Result<SubmissionStatusDto>> Handle(
        GetSubmissionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var submission = await _context.EInvoiceSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.Id &&
                                      s.TenantId == _tenantContext.TenantId &&
                                      !s.IsDeleted, cancellationToken);

        if (submission is null)
            return Result<SubmissionStatusDto>.Failure("Gönderim kaydı bulunamadı.", "SUBMISSION_NOT_FOUND");

        if (!string.IsNullOrWhiteSpace(submission.GibUuid))
        {
            var profile = await _context.EInvoiceProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted, cancellationToken);

            var provider = _providerResolver.Resolve(profile?.Provider ?? EInvoiceProvider.Stub);
            var statusResult = await provider.CheckStatusAsync(submission.GibUuid, cancellationToken);
            if (statusResult.Success)
            {
                submission.Status = EInvoiceSubmissionStatus.Accepted;
                submission.ResponseJson = statusResult.ResponseJson;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return Result<SubmissionStatusDto>.Success(new SubmissionStatusDto(
            submission.Id,
            submission.Status,
            submission.GibUuid,
            submission.ResponseJson,
            submission.ErrorMessage));
    }
}
