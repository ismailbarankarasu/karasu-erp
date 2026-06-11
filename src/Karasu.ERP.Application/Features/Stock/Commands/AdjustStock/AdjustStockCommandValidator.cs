using FluentValidation;

namespace Karasu.ERP.Application.Features.Stock.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.QuantityDelta).NotEqual(0).WithMessage("Miktar sıfır olamaz.");
    }
}
