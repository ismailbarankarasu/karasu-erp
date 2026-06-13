using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateExpense;

public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ExpenseDate).NotEmpty();
    }
}
