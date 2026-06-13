using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateCashTransaction;

public record CreateCashTransactionCommand(
    Guid CashRegisterId,
    CashTransactionType Type,
    decimal Amount,
    string? Description = null) : IRequest<Result<Guid>>;
