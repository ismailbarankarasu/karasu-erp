using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateFinancePayment;

public record CreateFinancePaymentCommand(
    FinancePaymentDirection Direction,
    decimal Amount,
    DateTime PaidAt,
    Guid? ReceivableId = null,
    Guid? PayableId = null,
    Guid? CashRegisterId = null,
    Guid? BankAccountId = null,
    string? ReferenceNo = null,
    string? Note = null) : IRequest<Result<Guid>>;
