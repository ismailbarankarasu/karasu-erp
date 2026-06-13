using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Customers.Queries.GetCustomerPaymentHistory;

public record GetCustomerPaymentHistoryQuery(Guid CustomerId) : IRequest<Result<IReadOnlyList<CustomerPaymentDto>>>;

public record CustomerPaymentDto(
    Guid Id,
    FinancePaymentDirection Direction,
    PaymentMethod Method,
    decimal Amount,
    DateTime PaidAt,
    string? ReferenceNo,
    string? Note,
    Guid? OrderId,
    Guid? InvoiceId);
