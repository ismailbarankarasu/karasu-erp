using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.EInvoice.Commands.SubmitEDispatch;

public record SubmitEDispatchCommand(Guid OrderId) : IRequest<Result<Guid>>;

public class SubmitEDispatchCommandHandler : IRequestHandler<SubmitEDispatchCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IEInvoiceProviderResolver _providerResolver;

    public SubmitEDispatchCommandHandler(
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

    public async Task<Result<Guid>> Handle(SubmitEDispatchCommand request, CancellationToken cancellationToken)
    {
        var profile = await _context.EInvoiceProfiles
            .FirstOrDefaultAsync(p => p.TenantId == _tenantContext.TenantId && !p.IsDeleted && p.IsActive, cancellationToken);
        if (profile is null)
            return Result<Guid>.Failure("E-Fatura profili yapılandırılmamış.", "EINVOICE_PROFILE_NOT_CONFIGURED");

        var order = await _context.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId &&
                                      o.TenantId == _tenantContext.TenantId &&
                                      !o.IsDeleted, cancellationToken);
        if (order is null)
            return Result<Guid>.Failure("Sipariş bulunamadı.", "ORDER_NOT_FOUND");

        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Shipping or OrderStatus.Delivered))
            return Result<Guid>.Failure("Sipariş durumu e-irsaliye için uygun değil.", "ORDER_STATUS_INVALID");

        var dispatchNumber = $"IRS-{DateTime.UtcNow:yyyyMMdd}-{order.OrderNumber}";
        var provider = _providerResolver.Resolve(profile.Provider);
        var submitRequest = new EInvoiceSubmitRequest(
            _tenantContext.TenantId,
            null,
            order.Id,
            dispatchNumber,
            order.GrandTotal,
            order.Customer?.FullName ?? "Müşteri",
            order.Customer?.TaxNumber);

        var result = await provider.SubmitEDispatchAsync(submitRequest, cancellationToken);

        var dispatch = new EDispatchNote
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrderId = order.Id,
            DispatchNumber = dispatchNumber,
            Status = result.Success ? EDispatchStatus.Accepted : EDispatchStatus.Rejected,
            GibUuid = result.GibUuid,
            ResponseJson = result.ResponseJson,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.EDispatchNotes.Add(dispatch);

        var submission = new EInvoiceSubmission
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrderId = order.Id,
            Type = EInvoiceSubmissionType.EDispatch,
            Status = result.Success ? EInvoiceSubmissionStatus.Accepted : EInvoiceSubmissionStatus.Failed,
            GibUuid = result.GibUuid,
            ResponseJson = result.ResponseJson,
            ErrorMessage = result.ErrorMessage,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _context.EInvoiceSubmissions.Add(submission);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Success
            ? Result<Guid>.Success(dispatch.Id)
            : Result<Guid>.Failure(result.ErrorMessage ?? "E-İrsaliye gönderimi başarısız.", "EDISPATCH_SUBMIT_FAILED");
    }
}
