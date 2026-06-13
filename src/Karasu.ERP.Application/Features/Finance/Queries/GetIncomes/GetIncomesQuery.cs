using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetIncomes;

public record GetIncomesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<Result<PaginatedList<IncomeDto>>>;

public record IncomeDto(
    Guid Id,
    Guid? CategoryId,
    string? CategoryName,
    decimal Amount,
    string Description,
    DateTime IncomeDate,
    string? Source,
    Guid? CashRegisterId,
    Guid? BankAccountId,
    DateTime CreatedAt);
