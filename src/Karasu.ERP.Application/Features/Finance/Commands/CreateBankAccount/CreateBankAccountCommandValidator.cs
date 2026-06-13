using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateBankAccount;

public class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Iban).MaximumLength(34);
        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.OpeningBalance.HasValue);
    }
}
