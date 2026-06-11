using FluentValidation;

namespace Karasu.ERP.Application.Features.Pos.Commands.OpenPosSession;

public class OpenPosSessionCommandValidator : AbstractValidator<OpenPosSessionCommand>
{
    public OpenPosSessionCommandValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}
