using FluentValidation;

namespace Karasu.ERP.Application.Features.Pos.Commands.CreatePosReturn;

public class CreatePosReturnCommandValidator : AbstractValidator<CreatePosReturnCommand>
{
    public CreatePosReturnCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.OriginalOrderId).NotEmpty();
    }
}
