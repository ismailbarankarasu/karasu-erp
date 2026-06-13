using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateBankTransaction;

public class CreateBankTransactionCommandValidator : AbstractValidator<CreateBankTransactionCommand>
{
    public CreateBankTransactionCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ReferenceNo).MaximumLength(100);
    }
}
