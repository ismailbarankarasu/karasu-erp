using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetPayables;

public record GetPayablesQuery(PayableStatus? Status = null) : IRequest<Result<List<PayableDto>>>;

public record PayableDto(
    Guid Id,
    string CreditorName,
    Guid? SupplierId,
    decimal Amount,
    decimal PaidAmount,
    decimal RemainingAmount,
    DateTime DueDate,
    PayableStatus Status,
    string? Description);
