using FluentValidation;

namespace Karasu.ERP.Application.Features.Finance.Commands.CreateCashRegister;

public class CreateCashRegisterCommandValidator : AbstractValidator<CreateCashRegisterCommand>
{
    public CreateCashRegisterCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0)
            .When(x => x.OpeningBalance.HasValue);
    }
}
