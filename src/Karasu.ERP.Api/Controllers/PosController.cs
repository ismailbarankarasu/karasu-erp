using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Pos.Commands.ClosePosSession;
using Karasu.ERP.Application.Features.Pos.Commands.CreatePosSale;
using Karasu.ERP.Application.Features.Pos.Commands.OpenPosSession;
using Karasu.ERP.Application.Features.Pos.Queries.GetCurrentPosSession;
using Karasu.ERP.Application.Features.Pos.Queries.SearchPosProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/pos")]
public class PosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PosController(IMediator mediator) => _mediator = mediator;

    [HttpPost("sessions/open")]
    [Authorize(Policy = Policies.PosSessionOpen)]
    public async Task<IActionResult> OpenSession([FromBody] OpenPosSessionCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("sessions/current")]
    [Authorize(Policy = Policies.PosSessionView)]
    public async Task<IActionResult> GetCurrentSession(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCurrentPosSessionQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("sessions/{id:guid}/close")]
    [Authorize(Policy = Policies.PosSessionClose)]
    public async Task<IActionResult> CloseSession(Guid id, [FromBody] ClosePosSessionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ClosePosSessionCommand(id, request.ClosingBalance), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("sales")]
    [Authorize(Policy = Policies.PosSaleSell)]
    public async Task<IActionResult> CreateSale([FromBody] CreatePosSaleCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("products/search")]
    [Authorize(Policy = Policies.PosSessionView)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? barcode,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new SearchPosProductsQuery(barcode, search), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record ClosePosSessionRequest(decimal ClosingBalance);
