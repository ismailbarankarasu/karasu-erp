using ClosedXML.Excel;
using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Domain.Entities;
using Karasu.ERP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Karasu.ERP.Infrastructure.Services;

public class ProductExcelService : IProductExcelService
{
    private readonly IApplicationDbContext _context;

    public ProductExcelService(IApplicationDbContext context) => _context = context;

    public async Task<ProductImportResult> ImportAsync(Stream excelStream, Guid tenantId, CancellationToken ct)
    {
        var errors = new List<string>();
        var imported = 0;
        var skipped = 0;

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];

        var defaultUnitId = await _context.Units
            .Where(u => u.TenantId == tenantId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (defaultUnitId == Guid.Empty)
            return new ProductImportResult(0, 0, ["Varsayılan birim bulunamadı."]);

        foreach (var row in rows)
        {
            var sku = row.Cell(1).GetString().Trim();
            var name = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            if (await _context.Products.AnyAsync(p => p.TenantId == tenantId && p.Sku == sku && !p.IsDeleted, ct))
            {
                skipped++;
                errors.Add($"SKU zaten var: {sku}");
                continue;
            }

            var barcode = row.Cell(3).GetString().Trim();
            var salePrice = row.Cell(4).TryGetValue(out decimal price) ? price : 0m;
            var purchasePrice = row.Cell(5).TryGetValue(out decimal purchase) ? purchase : 0m;
            var minStock = row.Cell(6).TryGetValue(out decimal min) ? min : 0m;

            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Sku = sku,
                Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
                Name = name,
                UnitId = defaultUnitId,
                PurchasePrice = purchasePrice,
                SalePrice = salePrice,
                TaxRate = 20m,
                MinStock = minStock,
                Status = ProductStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var variant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductId = product.Id,
                Sku = product.Sku,
                Barcode = product.Barcode,
                PurchasePrice = product.PurchasePrice,
                SalePrice = product.SalePrice,
                AttributesJson = "{}",
                CreatedAt = DateTime.UtcNow
            };

            await _context.Products.AddAsync(product, ct);
            await _context.ProductVariants.AddAsync(variant, ct);
            imported++;
        }

        if (imported > 0)
            await _context.SaveChangesAsync(ct);

        return new ProductImportResult(imported, skipped, errors);
    }

    public async Task<byte[]> ExportAsync(Guid tenantId, CancellationToken ct)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Sku,
                p.Barcode,
                p.Name,
                p.SalePrice,
                p.PurchasePrice,
                p.MinStock,
                p.Status
            })
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Ürünler");
        sheet.Cell(1, 1).Value = "SKU";
        sheet.Cell(1, 2).Value = "Ad";
        sheet.Cell(1, 3).Value = "Barkod";
        sheet.Cell(1, 4).Value = "Satış Fiyatı";
        sheet.Cell(1, 5).Value = "Alış Fiyatı";
        sheet.Cell(1, 6).Value = "Min Stok";
        sheet.Cell(1, 7).Value = "Durum";

        var rowIndex = 2;
        foreach (var product in products)
        {
            sheet.Cell(rowIndex, 1).Value = product.Sku;
            sheet.Cell(rowIndex, 2).Value = product.Name;
            sheet.Cell(rowIndex, 3).Value = product.Barcode ?? string.Empty;
            sheet.Cell(rowIndex, 4).Value = product.SalePrice;
            sheet.Cell(rowIndex, 5).Value = product.PurchasePrice;
            sheet.Cell(rowIndex, 6).Value = product.MinStock;
            sheet.Cell(rowIndex, 7).Value = product.Status.ToString();
            rowIndex++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
