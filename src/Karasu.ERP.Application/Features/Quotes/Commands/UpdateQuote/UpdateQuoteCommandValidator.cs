using FluentValidation;

namespace Karasu.ERP.Application.Features.Quotes.Commands.UpdateQuote;

public class UpdateQuoteCommandValidator : AbstractValidator<UpdateQuoteCommand>
{
    public UpdateQuoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
    }
}
