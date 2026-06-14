using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.EInvoice.Commands.SubmitEArchive;
using Karasu.ERP.Application.Features.EInvoice.Commands.SubmitEDispatch;
using Karasu.ERP.Application.Features.EInvoice.Commands.SubmitEInvoice;
using Karasu.ERP.Application.Features.EInvoice.Commands.UpdateEInvoiceProfile;
using Karasu.ERP.Application.Features.EInvoice.Queries.GetEInvoiceProfile;
using Karasu.ERP.Application.Features.EInvoice.Queries.GetEInvoiceSubmissions;
using Karasu.ERP.Application.Features.EInvoice.Queries.GetSubmissionStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/einvoice")]
public class EInvoiceController : ControllerBase
{
    private readonly IMediator _mediator;

    public EInvoiceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("profile")]
    [Authorize(Policy = Policies.EInvoiceView)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEInvoiceProfileQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("profile")]
    [Authorize(Policy = Policies.EInvoiceConfigure)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateEInvoiceProfileCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess ? Ok(Wrap(new { id = result.Data })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("submit/{invoiceId:guid}")]
    [Authorize(Policy = Policies.EInvoiceSubmit)]
    public async Task<IActionResult> SubmitEInvoice(Guid invoiceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitEInvoiceCommand(invoiceId), ct);
        return result.IsSuccess ? Ok(Wrap(new { id = result.Data })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("archive/{invoiceId:guid}")]
    [Authorize(Policy = Policies.EInvoiceSubmit)]
    public async Task<IActionResult> SubmitEArchive(Guid invoiceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitEArchiveCommand(invoiceId), ct);
        return result.IsSuccess ? Ok(Wrap(new { id = result.Data })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("dispatch/{orderId:guid}")]
    [Authorize(Policy = Policies.EInvoiceSubmit)]
    public async Task<IActionResult> SubmitEDispatch(Guid orderId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitEDispatchCommand(orderId), ct);
        return result.IsSuccess ? Ok(Wrap(new { id = result.Data })) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("submissions")]
    [Authorize(Policy = Policies.EInvoiceView)]
    public async Task<IActionResult> GetSubmissions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEInvoiceSubmissionsQuery(page, pageSize), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("submissions/{id:guid}/status")]
    [Authorize(Policy = Policies.EInvoiceView)]
    public async Task<IActionResult> GetSubmissionStatus(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSubmissionStatusQuery(id), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : NotFound(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
