using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    InvoiceStatus? Status = null,
    Guid? CustomerId = null) : IRequest<Result<PaginatedList<InvoiceListDto>>>;

public record InvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    InvoiceType Type,
    InvoiceStatus Status,
    decimal GrandTotal,
    DateTime? IssuedAt,
    DateTime CreatedAt);
