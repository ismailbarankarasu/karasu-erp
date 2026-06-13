using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateIncome;

public class CreateIncomeCommandValidator : AbstractValidator<CreateIncomeCommand>
{
    public CreateIncomeCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IncomeDate).NotEmpty();
        RuleFor(x => x.Source).MaximumLength(200);
    }
}
