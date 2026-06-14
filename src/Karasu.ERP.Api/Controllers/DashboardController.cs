using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Dashboard.Common;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetBranchComparison;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetDashboardCriticalStock;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetDashboardSummary;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetPendingOrders;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetRecentActivities;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetRevenueExpense;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetSalesTrend;
using Karasu.ERP.Application.Features.Dashboard.Queries.GetTopProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("dashboard/summary")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/sales-trend")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetSalesTrend(
        [FromQuery] SalesTrendPeriod period = SalesTrendPeriod.Daily,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branchId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSalesTrendQuery(period, from, to, branchId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/revenue-expense")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetRevenueExpense(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRevenueExpenseQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/top-products")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetTopProducts([FromQuery] int? days = 30, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTopProductsQuery(days), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/branch-comparison")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetBranchComparison(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBranchComparisonQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/recent-activities")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetRecentActivities(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRecentActivitiesQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/critical-stock")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetCriticalStock(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardCriticalStockQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("dashboard/pending-orders")]
    [Authorize(Policy = Policies.DashboardView)]
    public async Task<IActionResult> GetPendingOrders(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPendingOrdersQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
