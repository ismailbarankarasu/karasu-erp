using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Reports.Queries.ExportReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetCustomerReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetIncomeExpenseReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetProductReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetProfitLossReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetSalesReport;
using Karasu.ERP.Application.Features.Reports.Queries.GetStockReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("sales")]
    [Authorize(Policy = Policies.ReportSalesView)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? branchId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSalesReportQuery(from, to, branchId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("profit-loss")]
    [Authorize(Policy = Policies.ReportFinanceView)]
    public async Task<IActionResult> GetProfitLossReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProfitLossReportQuery(from, to), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("income-expense")]
    [Authorize(Policy = Policies.ReportFinanceView)]
    public async Task<IActionResult> GetIncomeExpenseReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetIncomeExpenseReportQuery(from, to), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("customers")]
    [Authorize(Policy = Policies.ReportCustomerView)]
    public async Task<IActionResult> GetCustomerReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCustomerReportQuery(from, to), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("products")]
    [Authorize(Policy = Policies.ReportProductView)]
    public async Task<IActionResult> GetProductReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductReportQuery(from, to), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("stock")]
    [Authorize(Policy = Policies.ReportStockView)]
    public async Task<IActionResult> GetStockReport(
        [FromQuery] Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockReportQuery(warehouseId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("{type}/export")]
    [Authorize(Policy = Policies.ReportExport)]
    public async Task<IActionResult> ExportReport(
        string type,
        [FromQuery] string format = "csv",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? warehouseId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ExportReportQuery(type, format, from, to, branchId, warehouseId), ct);

        if (!result.IsSuccess)
            return BadRequest(WrapError(result.Error!, result.ErrorCode));

        return File(result.Data!.Content, result.Data.ContentType, result.Data.FileName);
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
