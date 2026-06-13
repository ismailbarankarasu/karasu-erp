using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Queries.GetExpenses;

public record GetExpensesQuery(
    int Page = 1,
    int PageSize = 20,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<Result<PaginatedList<ExpenseDto>>>;

public record ExpenseDto(
    Guid Id,
    Guid? CategoryId,
    string? CategoryName,
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    PaymentMethod PaymentMethod,
    Guid? CashRegisterId,
    Guid? BankAccountId,
    DateTime CreatedAt);
