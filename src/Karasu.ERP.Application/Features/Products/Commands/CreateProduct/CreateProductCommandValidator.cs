using FluentValidation;

namespace Karasu.ERP.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU zorunludur.")
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.UnitId)
            .NotEmpty().WithMessage("Birim seçimi zorunludur.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("Satış fiyatı sıfırdan büyük olmalıdır.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100);
    }
}
