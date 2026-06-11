using FluentValidation;

namespace Karasu.ERP.Application.Features.Pos.Commands.CreatePosSale;

public class CreatePosSaleCommandValidator : AbstractValidator<CreatePosSaleCommand>
{
    public CreatePosSaleCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("En az bir satış satırı gereklidir.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductVariantId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.TaxRate).InclusiveBetween(0, 100);
        });

        RuleFor(x => x.Payments)
            .NotEmpty()
            .WithMessage("En az bir ödeme gereklidir.");

        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(p => p.Amount).GreaterThan(0);
            payment.RuleFor(p => p.ChangeAmount).GreaterThanOrEqualTo(0);
        });
    }
}
