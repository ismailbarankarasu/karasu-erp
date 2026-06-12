using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Invoices.Commands.CreateInvoiceFromOrder;

public record CreateInvoiceFromOrderCommand(
    Guid OrderId,
    InvoiceType Type = InvoiceType.Standard,
    bool IssueImmediately = false) : IRequest<Result<Guid>>;
