using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetBankAccountTransactions;

public record GetBankAccountTransactionsQuery(
    Guid BankAccountId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PaginatedList<BankTransactionDto>>>;

public record BankTransactionDto(
    Guid Id,
    Guid BankAccountId,
    BankTransactionType Type,
    decimal Amount,
    string? Description,
    string? ReferenceNo,
    DateTime CreatedAt);
