using FluentValidation;

namespace Karasu.ERP.Application.Features.Stock.Commands.CreateStockCount;

public class CreateStockCountCommandValidator : AbstractValidator<CreateStockCountCommand>
{
    public CreateStockCountCommandValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty();
    }
}
