using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Stock.Commands.AdjustStock;
using Karasu.ERP.Application.Features.Stock.Commands.CompleteStockCount;
using Karasu.ERP.Application.Features.Stock.Commands.CompleteStockTransfer;
using Karasu.ERP.Application.Features.Stock.Commands.CreateStockCount;
using Karasu.ERP.Application.Features.Stock.Commands.CreateStockTransfer;
using Karasu.ERP.Application.Features.Stock.Commands.UpdateStockCountLines;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockAlerts;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockByVariant;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockCountById;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockCounts;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockItems;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockMovements;
using Karasu.ERP.Application.Features.Stock.Queries.GetStockTransfers;
using Karasu.ERP.Domain.Enums;
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

    [HttpGet("stock/{productVariantId:guid}")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockByVariant(Guid productVariantId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockByVariantQuery(productVariantId), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
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

    [HttpGet("stock/alerts")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockAlerts([FromQuery] Guid? warehouseId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockAlertsQuery(warehouseId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("stock/transfers")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockTransfers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] StockTransferStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockTransfersQuery(page, pageSize, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("stock/transfers")]
    [Authorize(Policy = Policies.StockTransferCreate)]
    public async Task<IActionResult> CreateStockTransfer([FromBody] CreateStockTransferCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPatch("stock/transfers/{id:guid}/complete")]
    [Authorize(Policy = Policies.StockTransferCreate)]
    public async Task<IActionResult> CompleteStockTransfer(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteStockTransferCommand(id), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Transfer tamamlandı." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("stock/counts")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockCounts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] StockCountStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockCountsQuery(page, pageSize, warehouseId, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("stock/counts/{id:guid}")]
    [Authorize(Policy = Policies.StockView)]
    public async Task<IActionResult> GetStockCount(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockCountByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("stock/counts")]
    [Authorize(Policy = Policies.StockCountCreate)]
    public async Task<IActionResult> CreateStockCount([FromBody] CreateStockCountCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("stock/counts/{id:guid}/lines")]
    [Authorize(Policy = Policies.StockCountCreate)]
    public async Task<IActionResult> UpdateStockCountLines(
        Guid id,
        [FromBody] UpdateStockCountLinesRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateStockCountLinesCommand(id, request.Lines), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sayım satırları güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("stock/counts/{id:guid}/complete")]
    [Authorize(Policy = Policies.StockCountCreate)]
    public async Task<IActionResult> CompleteStockCount(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteStockCountCommand(id), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sayım tamamlandı." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateStockCountLinesRequest(
    IReadOnlyList<Karasu.ERP.Application.Features.Stock.Commands.UpdateStockCountLines.StockCountLineUpdateDto> Lines);
