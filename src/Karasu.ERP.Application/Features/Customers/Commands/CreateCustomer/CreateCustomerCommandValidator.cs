using FluentValidation;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Application.Features.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Müşteri adı zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Şirket adı zorunludur.")
            .MaximumLength(200)
            .When(x => x.Type == CustomerType.Corporate);

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi numarası zorunludur.")
            .MaximumLength(20)
            .When(x => x.Type == CustomerType.Corporate);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
    }
}
