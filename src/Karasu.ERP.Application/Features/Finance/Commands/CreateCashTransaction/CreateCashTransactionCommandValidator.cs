using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateCashTransaction;

public class CreateCashTransactionCommandValidator : AbstractValidator<CreateCashTransactionCommand>
{
    public CreateCashTransactionCommandValidator()
    {
        RuleFor(x => x.CashRegisterId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
