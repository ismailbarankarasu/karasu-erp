using Karasu.ERP.Application.Features.EInvoice.Common;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.EInvoice.Commands.SubmitEInvoice;

public record SubmitEInvoiceCommand(Guid InvoiceId) : IRequest<Result<Guid>>;

public class SubmitEInvoiceCommandHandler : IRequestHandler<SubmitEInvoiceCommand, Result<Guid>>
{
    private readonly EInvoiceSubmissionHelper _helper;

    public SubmitEInvoiceCommandHandler(EInvoiceSubmissionHelper helper) => _helper = helper;

    public Task<Result<Guid>> Handle(SubmitEInvoiceCommand request, CancellationToken cancellationToken) =>
        _helper.SubmitInvoiceAsync(request.InvoiceId, EInvoiceSubmissionType.EInvoice, InvoiceType.EInvoice, cancellationToken);
}
