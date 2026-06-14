using Karasu.ERP.Application.Common.Interfaces;
using Karasu.ERP.Application.Features.Reports.Queries.GetCustomerReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetIncomeExpenseReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetProductReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetProfitLossReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetSalesReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetStockReport;
using Karasu.ERP.Shared.Models;
using MediatR;

namespace Karasu.ERP.Application.Features.Reports.Queries.ExportReport;

public class ExportReportQueryHandler : IRequestHandler<ExportReportQuery, Result<ReportExportDto>>
{
    private static readonly HashSet<string> ValidTypes =
    [
        "sales",
        "profit-loss",
        "income-expense",
        "customers",
        "products",
        "stock"
    ];

    private static readonly HashSet<string> ValidFormats = ["csv", "excel", "pdf"];

    private readonly IMediator _mediator;
    private readonly IReportExportService _exportService;

    public ExportReportQueryHandler(IMediator mediator, IReportExportService exportService)
    {
        _mediator = mediator;
        _exportService = exportService;
    }

    public async Task<Result<ReportExportDto>> Handle(
        ExportReportQuery request,
        CancellationToken cancellationToken)
    {
        var reportType = request.Type.Trim().ToLowerInvariant();
        var format = request.Format.Trim().ToLowerInvariant();

        if (!ValidTypes.Contains(reportType))
            return Result.Failure<ReportExportDto>("Geçersiz rapor türü.", "INVALID_REPORT_TYPE");

        if (!ValidFormats.Contains(format))
            return Result.Failure<ReportExportDto>("Geçersiz dışa aktarma formatı.", "INVALID_FORMAT");

        if (reportType != "stock" && (!request.FromDate.HasValue || !request.ToDate.HasValue))
            return Result.Failure<ReportExportDto>("Başlangıç ve bitiş tarihi zorunludur.", "DATE_REQUIRED");

        object reportData;
        switch (reportType)
        {
            case "sales":
            {
                var result = await _mediator.Send(
                    new GetSalesReportQuery(request.FromDate!.Value, request.ToDate!.Value, request.BranchId),
                    cancellationToken);
                if (!result.IsSuccess)
                    return Result.Failure<ReportExportDto>(result.Error!, result.ErrorCode);
                reportData = result.Data!;
                break;
            }
            case "profit-loss":
            {
                var result = await _mediator.Send(
                    new GetProfitLossReportQuery(request.FromDate!.Value, request.ToDate!.Value),
                    cancellationToken);
                if (!result.IsSuccess)
                    return Result.Failure<ReportExportDto>(result.Error!, result.ErrorCode);
                reportData = result.Data!;
                break;
            }
            case "income-expense":
            {
                var result = await _mediator.Send(
                    new GetIncomeExpenseReportQuery(request.FromDate!.Value, request.ToDate!.Value),
                    cancellationToken);
                if (!result.IsSuccess)
                    return Result.Failure<ReportExportDto>(result.Error!, result.ErrorCode);
                reportData = result.Data!;
                break;
            }
            case "customers":
            {
                var result = await _mediator.Send(
                    new GetCustomerReportQuery(request.FromDate!.Value, request.ToDate!.Value),
                    cancellationToken);
                if (!result.IsSuccess)
                    return Result.Failure<ReportExportDto>(result.Error!, result.ErrorCode);
                reportData = result.Data!;
                break;
            }
            case "products":
            {
                var result = await _mediator.Send(
                    new GetProductReportQuery(request.FromDate!.Value, request.ToDate!.Value),
                    cancellationToken);
                if (!result.IsSuccess)
                    return Result.Failure<ReportExportDto>(result.Error!, result.ErrorCode);
                reportData = result.Data!;
                break;
            }
            case "stock":
            {
                var result = await _mediator.Send(
                    new GetStockReportQuery(request.WarehouseId),
                    cancellationToken);
                if (!result.IsSuccess)
                    return Result.Failure<ReportExportDto>(result.Error!, result.ErrorCode);
                reportData = result.Data!;
                break;
            }
            default:
                return Result.Failure<ReportExportDto>("Geçersiz rapor türü.", "INVALID_REPORT_TYPE");
        }

        var export = await _exportService.ExportAsync(reportType, format, reportData, cancellationToken);

        return Result<ReportExportDto>.Success(new ReportExportDto(
            export.Content,
            export.ContentType,
            export.FileName));
    }
}
