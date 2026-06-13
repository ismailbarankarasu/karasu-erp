using Karasu.ERP.Domain.Enums;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateExpense;

public record CreateExpenseCommand(
    decimal Amount,
    string Description,
    DateTime ExpenseDate,
    PaymentMethod PaymentMethod,
    Guid? CategoryId = null,
    Guid? CashRegisterId = null,
    Guid? BankAccountId = null) : IRequest<Result<Guid>>;
