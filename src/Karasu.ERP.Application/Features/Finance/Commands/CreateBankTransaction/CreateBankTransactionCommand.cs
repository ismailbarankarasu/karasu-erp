using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateBankTransaction;

public record CreateBankTransactionCommand(
    Guid BankAccountId,
    BankTransactionType Type,
    decimal Amount,
    string? Description = null,
    string? ReferenceNo = null) : IRequest<Result<Guid>>;
