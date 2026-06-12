using FluentValidation;

namespace Karasu.ERP.Application.Features.Stock.Commands.UpdateStockCountLines;

public class UpdateStockCountLinesCommandValidator : AbstractValidator<UpdateStockCountLinesCommand>
{
    public UpdateStockCountLinesCommandValidator()
    {
        RuleFor(x => x.CountId).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.CountedQty).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l)
                .Must(l => l.LineId.HasValue || l.ProductVariantId.HasValue)
                .WithMessage("LineId veya ProductVariantId gereklidir.");
        });
    }
}
