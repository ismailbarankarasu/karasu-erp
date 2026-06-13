using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateFinancePayment;

public class CreateFinancePaymentCommandValidator : AbstractValidator<CreateFinancePaymentCommand>
{
    public CreateFinancePaymentCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaidAt).NotEmpty();
        RuleFor(x => x.ReferenceNo).MaximumLength(100);
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x)
            .Must(x => !(x.ReceivableId.HasValue && x.PayableId.HasValue))
            .WithMessage("Alacak ve borç aynı ödemede birlikte kullanılamaz.");
        RuleFor(x => x)
            .Must(x => !(x.CashRegisterId.HasValue && x.BankAccountId.HasValue))
            .WithMessage("Nakit kasa ve banka hesabı aynı ödemede birlikte kullanılamaz.");
    }
}
