using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Queries.GetCustomerReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetIncomeExpenseReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetProductReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetProfitLossReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetSalesReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetStockReport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Karasu.ERP.Infrastructure.Services;

public class ReportExportService : IReportExportService
{
    static ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<ReportExportResult> ExportAsync(
        string reportType,
        string format,
        object reportData,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = reportType.Trim().ToLowerInvariant();
        var normalizedFormat = format.Trim().ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        var result = normalizedFormat switch
        {
            "csv" => ExportCsv(normalizedType, reportData, timestamp),
            "excel" => ExportExcel(normalizedType, reportData, timestamp),
            "pdf" => ExportPdf(normalizedType, reportData, timestamp),
            _ => throw new ArgumentException($"Desteklenmeyen format: {format}", nameof(format))
        };

        return Task.FromResult(result);
    }

    private static ReportExportResult ExportCsv(string reportType, object reportData, string timestamp)
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF');

        switch (reportType)
        {
            case "sales":
                WriteSalesCsv(sb, (SalesReportDto)reportData);
                break;
            case "profit-loss":
                WriteProfitLossCsv(sb, (ProfitLossReportDto)reportData);
                break;
            case "income-expense":
                WriteIncomeExpenseCsv(sb, (IncomeExpenseReportDto)reportData);
                break;
            case "customers":
                WriteCustomersCsv(sb, (CustomerReportDto)reportData);
                break;
            case "products":
                WriteProductsCsv(sb, (ProductReportDto)reportData);
                break;
            case "stock":
                WriteStockCsv(sb, (StockReportDto)reportData);
                break;
            default:
                throw new ArgumentException($"Desteklenmeyen rapor türü: {reportType}", nameof(reportType));
        }

        return new ReportExportResult(
            Encoding.UTF8.GetBytes(sb.ToString()),
            "text/csv",
            $"{reportType}-report-{timestamp}.csv");
    }

    private static ReportExportResult ExportExcel(string reportType, object reportData, string timestamp)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(GetReportTitle(reportType));

        switch (reportType)
        {
            case "sales":
                WriteSalesExcel(sheet, (SalesReportDto)reportData);
                break;
            case "profit-loss":
                WriteProfitLossExcel(sheet, (ProfitLossReportDto)reportData);
                break;
            case "income-expense":
                WriteIncomeExpenseExcel(sheet, (IncomeExpenseReportDto)reportData);
                break;
            case "customers":
                WriteCustomersExcel(sheet, (CustomerReportDto)reportData);
                break;
            case "products":
                WriteProductsExcel(sheet, (ProductReportDto)reportData);
                break;
            case "stock":
                WriteStockExcel(sheet, (StockReportDto)reportData);
                break;
            default:
                throw new ArgumentException($"Desteklenmeyen rapor türü: {reportType}", nameof(reportType));
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ReportExportResult(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{reportType}-report-{timestamp}.xlsx");
    }

    private static ReportExportResult ExportPdf(string reportType, object reportData, string timestamp)
    {
        var title = GetReportTitle(reportType);
        var headers = GetHeaders(reportType);
        var rows = GetRows(reportType, reportData);
        var summaryLines = GetSummaryLines(reportType, reportData);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(title).Bold().FontSize(16);
                    column.Item().Text($"Oluşturulma: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC");
                });

                page.Content().PaddingVertical(12).Column(column =>
                {
                    foreach (var line in summaryLines)
                        column.Item().Text(line);

                    if (summaryLines.Count > 0)
                        column.Item().PaddingTop(8);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (var i = 0; i < headers.Count; i++)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var headerText in headers)
                                header.Cell().Text(headerText).Bold();
                        });

                        foreach (var row in rows)
                        {
                            foreach (var cell in row)
                                table.Cell().Text(cell);
                        }
                    });
                });
            });
        });

        return new ReportExportResult(
            document.GeneratePdf(),
            "application/pdf",
            $"{reportType}-report-{timestamp}.pdf");
    }

    private static string GetReportTitle(string reportType) => reportType switch
    {
        "sales" => "Satış Raporu",
        "profit-loss" => "Kar/Zarar Raporu",
        "income-expense" => "Gelir/Gider Raporu",
        "customers" => "Müşteri Raporu",
        "products" => "Ürün Satış Raporu",
        "stock" => "Stok Raporu",
        _ => "Rapor"
    };

    private static List<string> GetSummaryLines(string reportType, object reportData) => reportType switch
    {
        "sales" =>
        [
            $"Toplam Satış: {((SalesReportDto)reportData).TotalSales:N2} TL",
            $"Sipariş Sayısı: {((SalesReportDto)reportData).OrderCount}"
        ],
        "profit-loss" =>
        [
            $"Gelir: {((ProfitLossReportDto)reportData).Revenue:N2} TL",
            $"Satış Maliyeti: {((ProfitLossReportDto)reportData).Cogs:N2} TL",
            $"Giderler: {((ProfitLossReportDto)reportData).Expenses:N2} TL",
            $"Kar: {((ProfitLossReportDto)reportData).Profit:N2} TL"
        ],
        "income-expense" =>
        [
            $"Toplam Gelir: {((IncomeExpenseReportDto)reportData).TotalIncome:N2} TL",
            $"Toplam Gider: {((IncomeExpenseReportDto)reportData).TotalExpense:N2} TL",
            $"Net: {((IncomeExpenseReportDto)reportData).Net:N2} TL"
        ],
        _ => []
    };

    private static List<string> GetHeaders(string reportType) => reportType switch
    {
        "sales" => ["Sipariş No", "Tarih", "Şube", "Müşteri", "Toplam", "Durum"],
        "profit-loss" => ["Kalem", "Tutar"],
        "income-expense" => ["Ay", "Gelir", "Gider", "Net"],
        "customers" => ["Müşteri", "Sipariş Sayısı", "Toplam Harcama"],
        "products" => ["Ürün", "SKU", "Satılan Adet", "Gelir"],
        "stock" => ["Depo", "Ürün", "SKU", "Miktar", "Min Stok", "Kullanılabilir"],
        _ => []
    };

    private static List<List<string>> GetRows(string reportType, object reportData) => reportType switch
    {
        "sales" => ((SalesReportDto)reportData).Items
            .Select(i => new List<string>
            {
                i.OrderNumber,
                i.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                i.BranchName,
                i.CustomerName ?? "-",
                i.GrandTotal.ToString("N2", CultureInfo.InvariantCulture),
                i.Status.ToString()
            }).ToList(),
        "profit-loss" =>
        [
            ["Gelir", ((ProfitLossReportDto)reportData).Revenue.ToString("N2", CultureInfo.InvariantCulture)],
            ["Satış Maliyeti", ((ProfitLossReportDto)reportData).Cogs.ToString("N2", CultureInfo.InvariantCulture)],
            ["Giderler", ((ProfitLossReportDto)reportData).Expenses.ToString("N2", CultureInfo.InvariantCulture)],
            ["Kar", ((ProfitLossReportDto)reportData).Profit.ToString("N2", CultureInfo.InvariantCulture)]
        ],
        "income-expense" => ((IncomeExpenseReportDto)reportData).MonthlyBreakdown
            .Select(m => new List<string>
            {
                m.Month,
                m.Income.ToString("N2", CultureInfo.InvariantCulture),
                m.Expense.ToString("N2", CultureInfo.InvariantCulture),
                m.Net.ToString("N2", CultureInfo.InvariantCulture)
            }).ToList(),
        "customers" => ((CustomerReportDto)reportData).Items
            .Select(c => new List<string>
            {
                c.CustomerName,
                c.OrderCount.ToString(CultureInfo.InvariantCulture),
                c.TotalSpent.ToString("N2", CultureInfo.InvariantCulture)
            }).ToList(),
        "products" => ((ProductReportDto)reportData).Items
            .Select(p => new List<string>
            {
                p.ProductName,
                p.Sku,
                p.QuantitySold.ToString("N2", CultureInfo.InvariantCulture),
                p.Revenue.ToString("N2", CultureInfo.InvariantCulture)
            }).ToList(),
        "stock" => ((StockReportDto)reportData).Items
            .Select(s => new List<string>
            {
                s.WarehouseName,
                s.ProductName,
                s.Sku,
                s.Quantity.ToString("N2", CultureInfo.InvariantCulture),
                s.MinStock.ToString("N2", CultureInfo.InvariantCulture),
                s.Available.ToString("N2", CultureInfo.InvariantCulture)
            }).ToList(),
        _ => []
    };

    private static void WriteSalesCsv(StringBuilder sb, SalesReportDto data)
    {
        sb.AppendLine($"Toplam Satış,{FormatDecimal(data.TotalSales)}");
        sb.AppendLine($"Sipariş Sayısı,{data.OrderCount}");
        sb.AppendLine();
        sb.AppendLine("Sipariş No,Tarih,Şube,Müşteri,Toplam,Durum");
        foreach (var item in data.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.OrderNumber),
                EscapeCsv(item.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
                EscapeCsv(item.BranchName),
                EscapeCsv(item.CustomerName ?? "-"),
                FormatDecimal(item.GrandTotal),
                EscapeCsv(item.Status.ToString())));
        }
    }

    private static void WriteProfitLossCsv(StringBuilder sb, ProfitLossReportDto data)
    {
        sb.AppendLine("Kalem,Tutar");
        sb.AppendLine($"Gelir,{FormatDecimal(data.Revenue)}");
        sb.AppendLine($"Satış Maliyeti,{FormatDecimal(data.Cogs)}");
        sb.AppendLine($"Giderler,{FormatDecimal(data.Expenses)}");
        sb.AppendLine($"Kar,{FormatDecimal(data.Profit)}");
    }

    private static void WriteIncomeExpenseCsv(StringBuilder sb, IncomeExpenseReportDto data)
    {
        sb.AppendLine($"Toplam Gelir,{FormatDecimal(data.TotalIncome)}");
        sb.AppendLine($"Toplam Gider,{FormatDecimal(data.TotalExpense)}");
        sb.AppendLine($"Net,{FormatDecimal(data.Net)}");
        sb.AppendLine();
        sb.AppendLine("Ay,Gelir,Gider,Net");
        foreach (var month in data.MonthlyBreakdown)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(month.Month),
                FormatDecimal(month.Income),
                FormatDecimal(month.Expense),
                FormatDecimal(month.Net)));
        }
    }

    private static void WriteCustomersCsv(StringBuilder sb, CustomerReportDto data)
    {
        sb.AppendLine("Müşteri,Sipariş Sayısı,Toplam Harcama");
        foreach (var item in data.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.CustomerName),
                item.OrderCount.ToString(CultureInfo.InvariantCulture),
                FormatDecimal(item.TotalSpent)));
        }
    }

    private static void WriteProductsCsv(StringBuilder sb, ProductReportDto data)
    {
        sb.AppendLine("Ürün,SKU,Satılan Adet,Gelir");
        foreach (var item in data.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.ProductName),
                EscapeCsv(item.Sku),
                FormatDecimal(item.QuantitySold),
                FormatDecimal(item.Revenue)));
        }
    }

    private static void WriteStockCsv(StringBuilder sb, StockReportDto data)
    {
        sb.AppendLine("Depo,Ürün,SKU,Miktar,Min Stok,Kullanılabilir");
        foreach (var item in data.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.WarehouseName),
                EscapeCsv(item.ProductName),
                EscapeCsv(item.Sku),
                FormatDecimal(item.Quantity),
                FormatDecimal(item.MinStock),
                FormatDecimal(item.Available)));
        }
    }

    private static void WriteSalesExcel(IXLWorksheet sheet, SalesReportDto data)
    {
        sheet.Cell(1, 1).Value = "Toplam Satış";
        sheet.Cell(1, 2).Value = data.TotalSales;
        sheet.Cell(2, 1).Value = "Sipariş Sayısı";
        sheet.Cell(2, 2).Value = data.OrderCount;

        var row = 4;
        WriteExcelHeader(sheet, row, ["Sipariş No", "Tarih", "Şube", "Müşteri", "Toplam", "Durum"]);
        row++;

        foreach (var item in data.Items)
        {
            sheet.Cell(row, 1).Value = item.OrderNumber;
            sheet.Cell(row, 2).Value = item.Date;
            sheet.Cell(row, 3).Value = item.BranchName;
            sheet.Cell(row, 4).Value = item.CustomerName ?? "-";
            sheet.Cell(row, 5).Value = item.GrandTotal;
            sheet.Cell(row, 6).Value = item.Status.ToString();
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteProfitLossExcel(IXLWorksheet sheet, ProfitLossReportDto data)
    {
        WriteExcelHeader(sheet, 1, ["Kalem", "Tutar"]);
        sheet.Cell(2, 1).Value = "Gelir";
        sheet.Cell(2, 2).Value = data.Revenue;
        sheet.Cell(3, 1).Value = "Satış Maliyeti";
        sheet.Cell(3, 2).Value = data.Cogs;
        sheet.Cell(4, 1).Value = "Giderler";
        sheet.Cell(4, 2).Value = data.Expenses;
        sheet.Cell(5, 1).Value = "Kar";
        sheet.Cell(5, 2).Value = data.Profit;
        sheet.Columns().AdjustToContents();
    }

    private static void WriteIncomeExpenseExcel(IXLWorksheet sheet, IncomeExpenseReportDto data)
    {
        sheet.Cell(1, 1).Value = "Toplam Gelir";
        sheet.Cell(1, 2).Value = data.TotalIncome;
        sheet.Cell(2, 1).Value = "Toplam Gider";
        sheet.Cell(2, 2).Value = data.TotalExpense;
        sheet.Cell(3, 1).Value = "Net";
        sheet.Cell(3, 2).Value = data.Net;

        var row = 5;
        WriteExcelHeader(sheet, row, ["Ay", "Gelir", "Gider", "Net"]);
        row++;

        foreach (var month in data.MonthlyBreakdown)
        {
            sheet.Cell(row, 1).Value = month.Month;
            sheet.Cell(row, 2).Value = month.Income;
            sheet.Cell(row, 3).Value = month.Expense;
            sheet.Cell(row, 4).Value = month.Net;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteCustomersExcel(IXLWorksheet sheet, CustomerReportDto data)
    {
        WriteExcelHeader(sheet, 1, ["Müşteri", "Sipariş Sayısı", "Toplam Harcama"]);
        var row = 2;
        foreach (var item in data.Items)
        {
            sheet.Cell(row, 1).Value = item.CustomerName;
            sheet.Cell(row, 2).Value = item.OrderCount;
            sheet.Cell(row, 3).Value = item.TotalSpent;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteProductsExcel(IXLWorksheet sheet, ProductReportDto data)
    {
        WriteExcelHeader(sheet, 1, ["Ürün", "SKU", "Satılan Adet", "Gelir"]);
        var row = 2;
        foreach (var item in data.Items)
        {
            sheet.Cell(row, 1).Value = item.ProductName;
            sheet.Cell(row, 2).Value = item.Sku;
            sheet.Cell(row, 3).Value = item.QuantitySold;
            sheet.Cell(row, 4).Value = item.Revenue;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteStockExcel(IXLWorksheet sheet, StockReportDto data)
    {
        WriteExcelHeader(sheet, 1, ["Depo", "Ürün", "SKU", "Miktar", "Min Stok", "Kullanılabilir"]);
        var row = 2;
        foreach (var item in data.Items)
        {
            sheet.Cell(row, 1).Value = item.WarehouseName;
            sheet.Cell(row, 2).Value = item.ProductName;
            sheet.Cell(row, 3).Value = item.Sku;
            sheet.Cell(row, 4).Value = item.Quantity;
            sheet.Cell(row, 5).Value = item.MinStock;
            sheet.Cell(row, 6).Value = item.Available;
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private static void WriteExcelHeader(IXLWorksheet sheet, int row, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
