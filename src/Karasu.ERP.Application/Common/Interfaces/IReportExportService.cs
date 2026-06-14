namespace Karasu.ERP.Application.Common.Interfaces;

public interface IReportExportService
{
    Task<ReportExportResult> ExportAsync(
        string reportType,
        string format,
        object reportData,
        CancellationToken cancellationToken = default);
}

public record ReportExportResult(byte[] Content, string ContentType, string FileName);
