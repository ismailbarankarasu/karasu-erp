using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.EInvoice.Common;

public class EInvoiceSubmissionHelper
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IEInvoiceProviderResolver _providerResolver;

    public EInvoiceSubmissionHelper(
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IEInvoiceProviderResolver providerResolver)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _providerResolver = providerResolver;
    }

    public async Task<Result<Guid>> SubmitInvoiceAsync(
        Guid invoiceId,
        EInvoiceSubmissionType submissionType,
        InvoiceType invoiceType,
        CancellationToken cancellationToken)
    {
        var profile = await _context.EInvoiceProfiles
            .FirstOrDefaultAsync(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted && p.IsActive, cancellationToken);
        if (profile is null)
            return Result<Guid>.Failure("E-Fatura profili yapılandırılmamış.", "EINVOICE_PROFILE_NOT_CONFIGURED");

        var invoice = await _context.Invoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.Id == invoiceId &&
                                        i.TenantId == _tenantContext.TenantId &&
                                        !i.IsDeleted, cancellationToken);
        if (invoice is null)
            return Result<Guid>.Failure("Fatura bulunamadı.", "INVOICE_NOT_FOUND");

        if (invoice.Status != InvoiceStatus.Issued)
            return Result<Guid>.Failure("Sadece kesilmiş faturalar gönderilebilir.", "INVOICE_NOT_ISSUED");

        var provider = _providerResolver.Resolve(profile.Provider);
        var submitRequest = new EInvoiceSubmitRequest(
            _tenantContext.TenantId,
            invoice.Id,
            invoice.OrderId,
            invoice.InvoiceNumber,
            invoice.GrandTotal,
            invoice.Customer.FullName,
            invoice.Customer.TaxNumber);

        var result = submissionType switch
        {
            EInvoiceSubmissionType.EInvoice => await provider.SubmitEInvoiceAsync(submitRequest, cancellationToken),
            EInvoiceSubmissionType.EArchive => await provider.SubmitEArchiveAsync(submitRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(submissionType))
        };

        var submission = new EInvoiceSubmission
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            InvoiceId = invoice.Id,
            OrderId = invoice.OrderId,
            Type = submissionType,
            Status = result.Success ? EInvoiceSubmissionStatus.Accepted : EInvoiceSubmissionStatus.Failed,
            GibUuid = result.GibUuid,
            ResponseJson = result.ResponseJson,
            ErrorMessage = result.ErrorMessage,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.EInvoiceSubmissions.Add(submission);

        if (result.Success)
            invoice.Type = invoiceType;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Success
            ? Result<Guid>.Success(submission.Id)
            : Result<Guid>.Failure(result.ErrorMessage ?? "Gönderim başarısız.", "EINVOICE_SUBMIT_FAILED");
    }
}
