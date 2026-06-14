namespace Karasu.ERP.Application.Common.Interfaces;

public record BarcodeGenerationResult(string Barcode, string? QrCodeBase64Png);

public interface IBarcodeService
{
    string GenerateUniqueBarcode(Guid tenantId, string sku);
    string GenerateQrCodeBase64(string content);
    string GenerateBarcodeImageBase64(string barcode);
}

public interface IProductExcelService
{
    Task<ProductImportResult> ImportAsync(Stream excelStream, Guid tenantId, CancellationToken ct);
    Task<byte[]> ExportAsync(Guid tenantId, CancellationToken ct);
}

public record ProductImportResult(int ImportedCount, int SkippedCount, IReadOnlyList<string> Errors);

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken ct);
    Task DeleteAsync(string storagePath, CancellationToken ct);
}
