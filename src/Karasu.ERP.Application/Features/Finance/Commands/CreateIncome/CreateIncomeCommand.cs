using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateIncome;

public record CreateIncomeCommand(
    decimal Amount,
    string Description,
    DateTime IncomeDate,
    Guid? CategoryId = null,
    string? Source = null,
    Guid? CashRegisterId = null,
    Guid? BankAccountId = null) : IRequest<Result<Guid>>;
