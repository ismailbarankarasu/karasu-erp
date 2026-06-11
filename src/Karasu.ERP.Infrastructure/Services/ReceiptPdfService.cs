using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Karasu.ERP.Infrastructure.Services;

public class ReceiptPdfService : IReceiptPdfService
{
    static ReceiptPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(PosReceiptData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(data.CompanyName).Bold().FontSize(16);
                    column.Item().Text(data.BranchName).SemiBold();
                    if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                        column.Item().Text(data.BranchAddress);
                    column.Item().PaddingTop(8).Text($"Fiş No: {data.OrderNumber}");
                    column.Item().Text($"Tarih: {data.OrderDate:dd.MM.yyyy HH:mm}");
                    if (!string.IsNullOrWhiteSpace(data.CustomerName))
                        column.Item().Text($"Müşteri: {data.CustomerName}");
                });

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Ürün").Bold();
                            header.Cell().AlignRight().Text("Adet").Bold();
                            header.Cell().AlignRight().Text("Birim").Bold();
                            header.Cell().AlignRight().Text("Toplam").Bold();
                        });

                        foreach (var line in data.Lines)
                        {
                            table.Cell().Text(line.ProductName);
                            table.Cell().AlignRight().Text(line.Quantity.ToString("N2"));
                            table.Cell().AlignRight().Text(line.UnitPrice.ToString("N2"));
                            table.Cell().AlignRight().Text(line.LineTotal.ToString("N2"));
                        }
                    });

                    column.Item().PaddingTop(12).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Ara Toplam: {data.SubTotal:N2} TL");
                        totals.Item().Text($"İndirim: {data.DiscountTotal:N2} TL");
                        totals.Item().Text($"KDV: {data.TaxTotal:N2} TL");
                        totals.Item().Text($"Genel Toplam: {data.GrandTotal:N2} TL").Bold();
                    });

                    if (data.Payments.Count > 0)
                    {
                        column.Item().PaddingTop(12).Text("Ödemeler").SemiBold();
                        foreach (var payment in data.Payments)
                        {
                            var label = FormatPaymentMethod(payment.Method);
                            var changeText = payment.ChangeAmount > 0
                                ? $" (Para üstü: {payment.ChangeAmount:N2} TL)"
                                : string.Empty;
                            column.Item().Text($"{label}: {payment.Amount:N2} TL{changeText}");
                        }
                    }
                });

                page.Footer().AlignCenter().Text("Teşekkür ederiz.");
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Nakit",
        PaymentMethod.CreditCard => "Kredi Kartı",
        PaymentMethod.BankTransfer => "Havale/EFT",
        PaymentMethod.Credit => "Veresiye",
        _ => method.ToString()
    };
}
