using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Stock.Commands.AdjustStock;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockItems;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockMovements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1")]
public class StockController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stock")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockItemsQuery(page, pageSize, warehouseId, search), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("stock/movements")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? productVariantId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStockMovementsQuery(page, pageSize, warehouseId, productVariantId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("stock/adjust")]
    [Authorize(Policy = Policies.StockAdjust)]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Stok güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
