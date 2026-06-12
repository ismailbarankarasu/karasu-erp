using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Shared.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Application.Features.Invoices.Commands.IssueInvoice;

public class IssueInvoiceCommandHandler : IRequestHandler<IssueInvoiceCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IUnitOfWork _unitOfWork;

    public IssueInvoiceCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _tenantContext = tenantContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(
                i => i.Id == request.InvoiceId && i.TenantId == _tenantContext.TenantId && !i.IsDeleted,
                cancellationToken);

        if (invoice is null)
            return Result.Failure("Fatura bulunamadı.", "INVOICE_NOT_FOUND");

        try
        {
            invoice.Issue();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message, "INVOICE_INVALID_STATUS");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
