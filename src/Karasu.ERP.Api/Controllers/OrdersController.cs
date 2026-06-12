using Karasu.ERP.Api.Authorization;
using Karasu.ERP.Api.Configuration;
using Karasu.ERP.Application.Features.Invoices.Commands.CreateInvoiceFromOrder;
using Karasu.ERP.Application.Features.Orders.Commands.CancelOrder;
using Karasu.ERP.Application.Features.Orders.Commands.ChangeOrderStatus;
using Karasu.ERP.Application.Features.Orders.Commands.ConfirmOrder;
using Karasu.ERP.Application.Features.Orders.Commands.CreateOrder;
using Karasu.ERP.Application.Features.Orders.Commands.DeleteOrder;
using Karasu.ERP.Application.Features.Orders.Commands.UpdateOrder;
using Karasu.ERP.Application.Features.Orders.Queries.GetBranches;
using Karasu.ERP.Application.Features.Orders.Queries.GetOrderById;
using Karasu.ERP.Application.Features.Orders.Queries.GetOrderHistory;
using Karasu.ERP.Application.Features.Orders.Queries.GetOrders;
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
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("orders")]
    [Authorize(Policy = Policies.OrderView)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetOrdersQuery(page, pageSize, status, customerId, branchId, fromDate, toDate, search), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("orders/{id:guid}")]
    [Authorize(Policy = Policies.OrderView)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPost("orders")]
    [Authorize(Policy = Policies.OrderCreate)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetOrder), new { id = result.Data }, Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPut("orders/{id:guid}")]
    [Authorize(Policy = Policies.OrderUpdate)]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateOrderCommand(id, request.CustomerId, request.Notes, request.Lines), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sipariş güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpDelete("orders/{id:guid}")]
    [Authorize(Policy = Policies.OrderDelete)]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteOrderCommand(id), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sipariş silindi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("orders/{id:guid}/history")]
    [Authorize(Policy = Policies.OrderView)]
    public async Task<IActionResult> GetOrderHistory(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrderHistoryQuery(id), ct);
        if (!result.IsSuccess) return NotFound(WrapError(result.Error!, result.ErrorCode));
        return Ok(Wrap(result.Data));
    }

    [HttpPatch("orders/{id:guid}/status")]
    [Authorize(Policy = Policies.OrderUpdate)]
    public async Task<IActionResult> ChangeOrderStatus(Guid id, [FromBody] ChangeOrderStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeOrderStatusCommand(id, request.Status, request.Note), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sipariş durumu güncellendi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("orders/{id:guid}/invoice")]
    [Authorize(Policy = Policies.InvoiceCreate)]
    public async Task<IActionResult> CreateInvoiceFromOrder(Guid id, [FromBody] CreateInvoiceFromOrderRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateInvoiceFromOrderCommand(
            id,
            request?.Type ?? InvoiceType.Standard,
            request?.IssueImmediately ?? false), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { id = result.Data }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("orders/{id:guid}/confirm")]
    [Authorize(Policy = Policies.OrderConfirm)]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmOrderCommand(id), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sipariş onaylandı." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpPost("orders/{id:guid}/cancel")]
    [Authorize(Policy = Policies.OrderCancel)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, request?.Reason), ct);
        return result.IsSuccess
            ? Ok(Wrap(new { message = "Sipariş iptal edildi." }))
            : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    [HttpGet("branches")]
    [Authorize(Policy = Policies.OrderView)]
    public async Task<IActionResult> GetBranches(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBranchesQuery(), ct);
        return result.IsSuccess ? Ok(Wrap(result.Data)) : BadRequest(WrapError(result.Error!, result.ErrorCode));
    }

    private static object Wrap<T>(T data) => new { success = true, data, errors = (object?)null };

    private static object WrapError(string message, string? code) =>
        new { success = false, data = (object?)null, errors = new[] { new { code = code ?? "ERROR", message } } };
}

public record CancelOrderRequest(string? Reason);

public record UpdateOrderRequest(
    Guid? CustomerId,
    string? Notes,
    List<Karasu.ERP.Application.Features.Orders.Commands.CreateOrder.CreateOrderLineDto> Lines);

public record ChangeOrderStatusRequest(OrderStatus Status, string? Note);

public record CreateInvoiceFromOrderRequest(InvoiceType? Type, bool? IssueImmediately);
