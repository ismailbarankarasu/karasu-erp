using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Quotes.Commands.ConvertQuoteToOrder;
using Karasu.ERP.Application.Features.Quotes.Commands.CreateQuote;
using Karasu.ERP.Application.Features.Quotes.Commands.UpdateQuote;
using Karasu.ERP.Application.Features.Quotes.Queries.GetQuoteById;
using Karasu.ERP.Application.Features.Quotes.Queries.GetQuotes;
using Karasu.ERP.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Karasu.ERP.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.ApiPolicy)]
[Route("api/v1/quotes")]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Policy = Policies.QuoteView)]
    public async Task<IActionResult> GetQuotes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] QuoteStatus? status = null,
        [FromQuery] Guid? customerId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetQuotesQuery(page, pageSize, status, customerId), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.QuoteView)]
    public async Task<IActionResult> GetQuote(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetQuoteByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost]
    [Authorize(Policy = Policies.QuoteCreate)]
    public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetQuote), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.QuoteUpdate)]
    public async Task<IActionResult> UpdateQuote(Guid id, [FromBody] UpdateQuoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateQuoteCommand(
            id, request.BranchId, request.CustomerId, request.Notes, request.ValidUntil, request.Lines), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Teklif güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("{id:guid}/convert")]
    [Authorize(Policy = Policies.QuoteConvert)]
    public async Task<IActionResult> ConvertToOrder(Guid id, [FromBody] ConvertQuoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConvertQuoteToOrderCommand(id, request.BranchId), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { orderId = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateQuoteRequest(
    Guid? BranchId,
    Guid? CustomerId,
    string? Notes,
    DateTime? ValidUntil,
    List<CreateQuoteLineDto> Lines);

public record ConvertQuoteRequest(Guid BranchId);
