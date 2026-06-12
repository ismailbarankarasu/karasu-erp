using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Warehouses.Commands.CreateWarehouse;
using Karasu.ERP.Application.Features.Warehouses.Commands.DeleteWarehouse;
using Karasu.ERP.Application.Features.Warehouses.Commands.UpdateWarehouse;
using Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouseById;
using Karasu.ERP.Application.Features.Warehouses.Queries.GetWarehouses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehousesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Policies.WarehouseView)]
    public async Task<IActionResult> GetWarehouses([FromQuery] Guid? branchId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWarehousesQuery(branchId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.WarehouseView)]
    public async Task<IActionResult> GetWarehouse(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWarehouseByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost]
    [Authorize(Policy = Policies.WarehouseCreate)]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetWarehouse), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.WarehouseUpdate)]
    public async Task<IActionResult> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateWarehouseCommand(id, request.Name, request.Code, request.IsDefault), ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Depo güncellendi." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.WarehouseDelete)]
    public async Task<IActionResult> DeleteWarehouse(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteWarehouseCommand(id), ct);
        return result.IsSuccess ? Ok(Wrap(new { message = "Depo silindi." })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateWarehouseRequest(string Name, string Code, bool IsDefault);
