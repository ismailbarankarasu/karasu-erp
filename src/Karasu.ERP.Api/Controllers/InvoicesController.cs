using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Invoices.Commands.IssueInvoice;
using Karasu.ERP.Application.Features.Invoices.Queries.GetInvoiceById;
using Karasu.ERP.Application.Features.Invoices.Queries.GetInvoices;
using Karasu.ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Policies.InvoiceView)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? customerId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetInvoicesQuery(page, pageSize, status, customerId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.InvoiceView)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInvoiceByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Policy = Policies.InvoiceCreate)]
    public async Task<IActionResult> IssueInvoice(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new IssueInvoiceCommand(id), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Fatura kesildi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}
