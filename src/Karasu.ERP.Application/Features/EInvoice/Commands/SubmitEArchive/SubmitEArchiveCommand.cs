using Karasu.ERP.Application.Features.EInvoice.Common;
using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.EInvoice.Commands.SubmitEArchive;

public record SubmitEArchiveCommand(Guid InvoiceId) : IRequest<Result<Guid>>;

public class SubmitEArchiveCommandHandler : IRequestHandler<SubmitEArchiveCommand, Result<Guid>>
{
    private readonly EInvoiceSubmissionHelper _helper;

    public SubmitEArchiveCommandHandler(EInvoiceSubmissionHelper helper) => _helper = helper;

    public Task<Result<Guid>> Handle(SubmitEArchiveCommand request, CancellationToken cancellationToken) =>
        _helper.SubmitInvoiceAsync(request.InvoiceId, EInvoiceSubmissionType.EArchive, InvoiceType.EArchive, cancellationToken);
}
