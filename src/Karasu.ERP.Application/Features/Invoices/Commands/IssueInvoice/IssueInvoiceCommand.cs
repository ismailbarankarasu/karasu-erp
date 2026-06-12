using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Invoices.Commands.IssueInvoice;

public record IssueInvoiceCommand(Guid InvoiceId) : IRequest<Result>;
