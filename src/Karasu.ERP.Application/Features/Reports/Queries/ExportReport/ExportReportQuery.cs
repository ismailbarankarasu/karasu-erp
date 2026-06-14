using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.ExportReport;

public record ExportReportQuery(
    string Type,
    string Format,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    Guid? BranchId = null,
    Guid? WarehouseId = null) : IRequest<Result<ReportExportDto>>;

public record ReportExportDto(byte[] Content, string ContentType, string FileName);
