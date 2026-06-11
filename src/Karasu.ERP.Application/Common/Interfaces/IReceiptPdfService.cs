using Karasu.ERP.Domain.Enums;

namespace Karasu.ERP.Application.Common.Interfaces;

public interface IReceiptPdfService
{
    byte[] Generate(PosReceiptData data);
}

public record PosReceiptData(
    string CompanyName,
    string BranchName,
    string? BranchAddress,
    string OrderNumber,
    DateTime OrderDate,
    string? CustomerName,
    IReadOnlyList<PosReceiptLineData> Lines,
    decimal SubTotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    IReadOnlyList<PosReceiptPaymentData> Payments);

public record PosReceiptLineData(
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal Discount,
    decimal LineTotal);

public record PosReceiptPaymentData(
    PaymentMethod Method,
    decimal Amount,
    decimal ChangeAmount);
