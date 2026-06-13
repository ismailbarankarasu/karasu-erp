using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetReceivables;

public record GetReceivablesQuery(
    Guid? CustomerId = null,
    ReceivableStatus? Status = null) : IRequest<Result<List<ReceivableDto>>>;

public record ReceivableDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? InvoiceId,
    Guid? OrderId,
    decimal Amount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime DueDate,
    ReceivableStatus Status,
    string? Description);
