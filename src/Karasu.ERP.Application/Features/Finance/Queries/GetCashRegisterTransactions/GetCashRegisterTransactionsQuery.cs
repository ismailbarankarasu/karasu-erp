using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetCashRegisterTransactions;

public record GetCashRegisterTransactionsQuery(
    Guid CashRegisterId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<CashTransactionDto>>>;

public record CashTransactionDto(
    Guid Id,
    Guid CashRegisterId,
    CashTransactionType Type,
    decimal Amount,
    string? Description,
    string? ReferenceType,
    Guid? ReferenceId,
    DateTime CreatedAt);
