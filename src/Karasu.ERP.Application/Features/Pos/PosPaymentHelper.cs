using Karasu.ERP.Application.Features.Pos.Commands.CreatePosSale;
using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Application.Features.Pos;

internal static class PosPaymentHelper
{
    public static (bool IsValid, string? Error, string? ErrorCode, List<PosPaymentDto> NormalizedPayments) ValidateAndNormalize(
        IReadOnlyList<PosPaymentDto> payments,
        decimal grandTotal,
        Guid? customerId)
    {
        if (payments.Count == 0)
            return (false, "En az bir ödeme gereklidir.", "PAYMENT_REQUIRED", []);

        var normalized = new List<PosPaymentDto>();
        var appliedTotal = 0m;

        foreach (var payment in payments)
        {
            var changeAmount = payment.ChangeAmount;

            if (payment.Method == PaymentMethod.Cash && payment.TenderedAmount.HasValue)
            {
                if (payment.TenderedAmount.Value < payment.Amount)
                    return (false, "Nakit ödemede verilen tutar, uygulanan tutardan küçük olamaz.", "INVALID_TENDERED_AMOUNT", []);

                changeAmount = payment.TenderedAmount.Value - payment.Amount;
            }

            if (payment.Method == PaymentMethod.Credit && !customerId.HasValue)
                return (false, "Veresiye ödeme için müşteri seçilmelidir.", "CUSTOMER_REQUIRED_FOR_CREDIT", []);

            normalized.Add(payment with { ChangeAmount = changeAmount });
            appliedTotal += payment.Amount;
        }

        if (appliedTotal != grandTotal)
        {
            return (false,
                $"Ödeme tutarı ({appliedTotal:N2}) sipariş toplamına ({grandTotal:N2}) eşit olmalıdır.",
                "PAYMENT_MISMATCH",
                []);
        }

        var cashPayments = normalized.Where(p => p.Method == PaymentMethod.Cash).ToList();
        if (cashPayments.Count > 1 && cashPayments.Any(p => p.TenderedAmount.HasValue))
            return (false, "Birden fazla nakit ödemede tenderedAmount kullanılamaz.", "INVALID_MULTI_CASH_TENDER", []);

        return (true, null, null, normalized);
    }
}
