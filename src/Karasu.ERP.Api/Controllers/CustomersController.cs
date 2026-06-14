using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Customers.Commands.AddCustomerNote;
using Karasu.ERP.Application.Features.Customers.Commands.CreateCustomer;
using Karasu.ERP.Application.Features.Customers.Commands.DeleteCustomer;
using Karasu.ERP.Application.Features.Customers.Commands.UpdateCustomer;
using Karasu.ERP.Application.Features.Customers.Commands.UploadCustomerAttachment;
using Karasu.ERP.Application.Features.Customers.Queries.GetCustomerAttachments;
using Karasu.ERP.Application.Features.Customers.Queries.GetCustomerById;
using Karasu.ERP.Application.Features.Customers.Queries.GetCustomerNotes;
using Karasu.ERP.Application.Features.Customers.Queries.GetCustomerOrders;
using Karasu.ERP.Application.Features.Customers.Queries.GetCustomerPaymentHistory;
using Karasu.ERP.Application.Features.Customers.Queries.GetCustomers;
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
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("customers")]
    [Authorize(Policy = Policies.CustomerView)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] CustomerType? type = null,
        [FromQuery] CustomerStatus? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCustomersQuery(page, pageSize, search, type, status), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("customers/{id:guid}")]
    [Authorize(Policy = Policies.CustomerView)]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("customers")]
    [Authorize(Policy = Policies.CustomerCreate)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCustomer), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("customers/{id:guid}")]
    [Authorize(Policy = Policies.CustomerUpdate)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCustomerCommand(
            id,
            request.Type,
            request.FullName,
            request.CompanyName,
            request.TaxNumber,
            request.Phone,
            request.Email,
            request.Address,
            request.City,
            request.CreditLimit,
            request.Status), ct);

        return result.IsSuccess
            ? Ok(Wrap(new { message = "Müşteri güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("customers/{id:guid}")]
    [Authorize(Policy = Policies.CustomerDelete)]
    public async Task<IActionResult> DeleteCustomer(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { message = "Müşteri silindi." }));
    }

    [HttpGet("customers/{id:guid}/notes")]
    [Authorize(Policy = Policies.CustomerView)]
    public async Task<IActionResult> GetCustomerNotes(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerNotesQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("customers/{id:guid}/notes")]
    [Authorize(Policy = Policies.CustomerUpdate)]
    public async Task<IActionResult> AddCustomerNote(Guid id, [FromBody] AddCustomerNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddCustomerNoteCommand(id, request.Content), ct);
        if (!result.IsSuccess) return BadRequest(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { id = result.Data }));
    }

    [HttpGet("customers/{id:guid}/payments")]
    [Authorize(Policy = Policies.CustomerView)]
    public async Task<IActionResult> GetCustomerPaymentHistory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerPaymentHistoryQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpGet("customers/{id:guid}/orders")]
    [Authorize(Policy = Policies.CustomerView)]
    public async Task<IActionResult> GetCustomerOrders(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCustomerOrdersQuery(id, page, pageSize), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpGet("customers/{id:guid}/attachments")]
    [Authorize(Policy = Policies.CustomerView)]
    public async Task<IActionResult> GetCustomerAttachments(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerAttachmentsQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("customers/{id:guid}/attachments")]
    [Authorize(Policy = Policies.CustomerUpdate)]
    public async Task<IActionResult> UploadCustomerAttachment(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(WrapError("Dosya boş.", "EMPTY_FILE"));

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadCustomerAttachmentCommand(
            id,
            stream,
            file.FileName,
            file.ContentType,
            file.Length), ct);

        if (!result.IsSuccess) return BadRequest(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(new { id = result.Data }));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record UpdateCustomerRequest(
    CustomerType Type,
    string FullName,
    string? CompanyName,
    string? TaxNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    decimal CreditLimit,
    CustomerStatus Status);

public record AddCustomerNoteRequest(string Content);
