using FluentValidation;

namespace Karasu.ERP.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.BranchId)
            .NotEmpty()
            .WithMessage("Şube seçimi zorunludur.");

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("En az bir sipariş satırı gereklidir.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductVariantId)
                .NotEmpty()
                .WithMessage("Ürün varyantı seçilmelidir.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0)
                .WithMessage("Miktar sıfırdan büyük olmalıdır.");

            line.RuleFor(l => l.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Birim fiyat negatif olamaz.");

            line.RuleFor(l => l.TaxRate)
                .InclusiveBetween(0, 100)
                .WithMessage("KDV oranı 0-100 arasında olmalıdır.");
        });
    }
}
