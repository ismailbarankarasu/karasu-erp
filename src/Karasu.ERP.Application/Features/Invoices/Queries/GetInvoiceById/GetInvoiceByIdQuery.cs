using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Invoices.Queries.GetInvoiceById;

public record GetInvoiceByIdQuery(Guid Id) : IRequest<Result<InvoiceDetailDto>>;

public record InvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid OrderId,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    InvoiceType Type,
    InvoiceStatus Status,
    decimal SubTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    DateTime? IssuedAt,
    DateTime CreatedAt,
    IReadOnlyList<InvoiceLineDto> Lines);

public record InvoiceLineDto(
    Guid Id,
    Guid? ProductVariantId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal LineTotal);
